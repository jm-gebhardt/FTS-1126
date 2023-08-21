using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Charging;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Choosers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Maintenance;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Parking;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;

/// <summary>
/// Base class for ActionManager which provides implementations for creating methods (Pick, Drop, Charge and Maintenance) and all base logic of all ActionManagers
/// </summary>
public abstract class BaseActionManager : IActionManager
{
    private readonly ISemaphoreProvider _semaphoreProvider;
    private readonly IBus _bus;
    private readonly IActionGroupRegistry _actionGroupRegistry;
    private readonly IRouteService _routeService;
    private readonly IChargingPointManager _chargingPointManager;
    private readonly IParkPointService _parkPointService;
    private readonly IAgvService _agvService;
    private readonly IChooseAgvActionGroupService _chooseAgvActionGroupService;
    private readonly IStartActionService _startActionService;

    /// <summary>
    /// Logger
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Maintenance Service
    /// </summary>
    protected IMaintenanceService MaintenanceService { get; }

    /// <summary>
    /// Agv Load Capability Service
    /// </summary>
    protected IAgvLoadCapabilityService AgvLoadCapabilityService { get; }

    /// <summary>
    /// Charging Service
    /// </summary>
    protected IChargingService ChargingService { get; }

    /// <summary>
    /// Agv State Service
    /// </summary>
    protected IAgvStateService AgvStateService { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="bus">Abstracted interface for Agv action registry</param>
    /// <param name="actionGroupRegistry">Abstracted interface for Agv action registry</param>
    /// <param name="routeService">Service for computation of Route for Agv</param>
    /// <param name="chargingPointManager">Finds and Reserves charging node for agv</param>
    /// <param name="parkPointService"></param>
    /// <param name="maintenanceService">Providing information about maintenance requests for Agvs</param>
    /// <param name="logger">generic interface for logging</param>
    /// <param name="chargingService"></param>
    /// <param name="agvService"></param>
    /// <param name="agvLoadCapabilityService"></param>
    /// <param name="startActionService"></param>
    /// <param name="agvStateService"></param>
    /// <param name="chooseAgvActionGroupService"></param>
    /// <param name="semaphoreProvider"></param>
    protected BaseActionManager(
        IBus bus,
        IActionGroupRegistry actionGroupRegistry,
        IRouteService routeService,
        IChargingPointManager chargingPointManager,
        IParkPointService parkPointService,
        IMaintenanceService maintenanceService,
        ILogger logger,
        IChargingService chargingService,
        IAgvService agvService,
        IAgvLoadCapabilityService agvLoadCapabilityService,
        IStartActionService startActionService,
        IAgvStateService agvStateService,
        IChooseAgvActionGroupService chooseAgvActionGroupService,
        ISemaphoreProvider semaphoreProvider)
    {
        _bus = bus;
        _actionGroupRegistry = actionGroupRegistry;
        _routeService = routeService;
        _chargingPointManager = chargingPointManager;
        _parkPointService = parkPointService;
        MaintenanceService = maintenanceService;
        Logger = logger;
        ChargingService = chargingService;
        _agvService = agvService;
        AgvLoadCapabilityService = agvLoadCapabilityService;
        AgvStateService = agvStateService;
        _chooseAgvActionGroupService = chooseAgvActionGroupService;
        _startActionService = startActionService;
        _semaphoreProvider = semaphoreProvider;
    }

    private static ActionGroup CreateActionGroup(Guid agvCorrelationId, string nodeId, string actionDescription, ActionGroupCreator creator, ActionType actionType = ActionType.Move)
    {
        var groupCorrelationId = Guid.NewGuid();
        var actionGroup = new ActionGroup(
            groupCorrelationId,
            $"Automatic {actionDescription}",
            actions: new List<AgvAction> {new(Guid.NewGuid(), actionType, nodeId, groupCorrelationId)},
            assignedAgv: agvCorrelationId, 
            creator:creator
        );

        return actionGroup;
    }

    private async Task<AgvAction> AddActionAsync(Guid agvCorrelationId, GraphNode node, ActionType actionType, ActionGroupCreator creator, string? actionDescription = null)
        => await AddActionAsync(agvCorrelationId, node.Id, actionType, creator, actionDescription);

    private async Task<AgvAction> AddActionAsync(Guid agvCorrelationId, string nodeId, ActionType actionType, ActionGroupCreator creator, string? actionDescription = null)
    {
        var actionGroup = CreateActionGroup(agvCorrelationId, nodeId, actionDescription ?? actionType.ToString(), creator, actionType);
        await AddActionGroupAsync(actionGroup);
        return actionGroup.Actions.First();
    }

    private async Task AddActionGroupAsync(ActionGroup actionGroup)
    {
        await _actionGroupRegistry.AddAsync(actionGroup);
        await _bus.Publish(new ActionGroupCreated(actionGroup, actionGroup.CorrelationId));
    }

    private async Task UpdateActionGroupAsync(AgvAction action, IAgv agv, ActionState state, CancellationToken cancellationToken)
    {
        var actionGroup = await _actionGroupRegistry.UpdateActionAndGroupStateAsync(action, agv, state);
        await _bus.Publish(new ActionGroupChanged(actionGroup, actionGroup.CorrelationId), cancellationToken);
    }

    private bool IsAgvCharging(IAgv agv) => agv.GetRequiredAgvStatus().Charging;

    private Task<IAgv> GetAgvAsync(Guid? correlationId, CancellationToken cancellationToken) => _agvService.GetAgvAsync(correlationId, cancellationToken);

    /// <summary>
    /// Chooses the best Agv available (which is confirmed to be ready) for the given <see cref="ActionGroup"/>
    /// </summary>
    /// <param name="actionGroup"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<IAgv?> GetReadyAgv(ActionGroup actionGroup, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Get ready Agv for ActionGroup with CorrelationId: {ActionGroupId}", actionGroup.CorrelationId);

        Guid? assignedAgv = actionGroup.AssignedAgv;

        if (assignedAgv != null)
        {
            Logger.LogDebug("The Agv is already assigned ActionGroup with CorrelationId: {ActionGroupId}, so verify it can be set to busy. The ActionGroup is Manual {IsManual}", actionGroup.CorrelationId, actionGroup.IsManual);

            // already assigned (e.g. manual ActionGroup)
            // we ignore the IsLocked for the manual ActionGroup
            var agvCorrelationId = (Guid) assignedAgv;
            return await _agvService.IsAgvReadyForBusyAsync(agvCorrelationId, cancellationToken, ignoreCharging: true, ignoreLocked: actionGroup.IsManual) ? await GetAgvAsync(agvCorrelationId, cancellationToken) : null;
        }

        List<IAgv> agvs = await GetReadyAndRecruitableAgvsAsync(cancellationToken);
        var agv = await _chooseAgvActionGroupService.GetAgvAsync(actionGroup, agvs);
        if (agv == null)
        {
            return null;
        }
        Logger.LogDebug("Found Agv for ActionGroup with CorrelationId: {ActionGroupId}", actionGroup.CorrelationId);

        return await GetAgvAsync(agv.CorrelationId, cancellationToken);
    }

    private async Task<List<IAgv>> GetReadyAndRecruitableAgvsAsync(CancellationToken cancellationToken)
    {
        // we take all ready Agvs
        Task<IEnumerable<IAgv>> readyAgvsTask = _agvService.GetReadyAgvsAsync(false, cancellationToken);

        List<IAgv> chargingAgvsWithGoodBatteryLevel = ChargingService.GetChargingAgvsWithGoodBatteryLevel();
        // + recruitable  (charging) Agvs
        Task<IEnumerable<IAgv>> recruitableChargingAgvsTasks = _agvService.GetReadyAgvsAsync(chargingAgvsWithGoodBatteryLevel, true, cancellationToken);

        IList<IAgv> normalReadyAgvs = (await readyAgvsTask).ToList();
        IList<IAgv> recruitableChargingAgvs = (await recruitableChargingAgvsTasks).ToList();
        Logger.LogDebug("Choosing Agv for ActionGroup out of: {NormalReadyAgvsCount} normal ready Agvs and {RecruitableChargingAgvsCount} recruitable charging Agvs", normalReadyAgvs.Count, recruitableChargingAgvs.Count);

        List<IAgv> agvs = normalReadyAgvs.Concat(recruitableChargingAgvs).ToList();
        return agvs;
    }

    /// <summary>
    /// Assigns a new action for Agv, but it does not use the semaphore
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <param name="cancellationToken"></param>
    private async Task AssignIfAgvReadyAsync(Guid agvCorrelationId, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Assigning action for Agv with CorrelationId: {AgvCorrelationId} if ready", agvCorrelationId);

        // check if agv is busy, it's ok if agv is locked
        if (!await _agvService.IsAgvReadyForBusyAsync(agvCorrelationId, cancellationToken, ignoreCharging: false, ignoreLocked: true))
        {
            // Agv might be charging
            Logger.LogDebug("Agv with CorrelationId {AgvCorrelationId} is not ready to be set to busy", agvCorrelationId);
            return;
        }
        Logger.LogDebug("Assigning action for Agv with CorrelationId: {AgvCorrelationId} because it is ready", agvCorrelationId);

        await AssignAsync(agvCorrelationId, cancellationToken);
    }

    /// <summary>
    /// Gets next action and starts it. Does not check if the Agv is ready
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <param name="cancellationToken"></param>
    private async Task AssignAsync(Guid agvCorrelationId, CancellationToken cancellationToken)
    {
        var agv = await GetAgvAsync(agvCorrelationId, cancellationToken);
        var nextAgvAction = await GetNextActionAsync(agv);

        if (nextAgvAction != null)
        {
            Logger.LogDebug("Starting next: {ActionType} action for Agv with CorrelationId {AgvCorrelationId}", nextAgvAction.ActionType, agv.CorrelationId);
            await StartActionAsync(nextAgvAction, agv, cancellationToken);
        }
        else
        {
            Logger.LogDebug("There is no next action available for Agv with CorrelationId {AgvCorrelationId}", agv.CorrelationId);
        }
    }

    /// <summary>
    /// Executes the given action in a locked context where no one else can do anything on our ActionGroups
    /// </summary>
    /// <param name="action"></param>
    /// <param name="cancellationToken"></param>
    protected async Task ExecuteLockedAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        try
        {
            await _semaphoreProvider.Semaphore.WaitAsync(cancellationToken);
            await action();
        }
        finally
        {
            _semaphoreProvider.Semaphore.Release();
        }
    }

    /// <summary>
    /// Send <see cref="StartActionCommand"/> and set action to start>.
    /// </summary>
    [PublicAPI]
    protected async Task StartActionAsync(AgvAction action, IAgv assignedAgv, CancellationToken cancellationToken)
    {
        action.Nodes = (await _routeService.GetRouteAsync(assignedAgv, action.TargetNodeId)).Nodes;

        Logger.LogDebug("Starting action with CorrelationId: {ActionCorrelationId} for Agv {AgvCorrelationId}. Setting Agv to busy in next step.", action.CorrelationId, assignedAgv.CorrelationId);
        await AgvStateService.SetBusyAsync(assignedAgv, true, true, cancellationToken);

        try
        {
            await _startActionService.StartAction(assignedAgv, action, cancellationToken);
            Logger.LogDebug("Action with CorrelationId: {ActionCorrelationId} for Agv {AgvCorrelationId} has been started. Updating ActionGroup.State to Started in next step.", action.CorrelationId, assignedAgv.CorrelationId);
            await UpdateActionGroupAsync(action, assignedAgv, ActionState.Started, cancellationToken);

            Logger.LogTrace("Starting action done");
        }
        catch
        {
            // release Agv in case of error
            await AgvStateService.SetIdleAsync(assignedAgv.CorrelationId, cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Get the succeeding assigned action (in most cases this will be a drop action)
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    /// <exception cref="ActionNotFoundException"></exception>
    protected async Task<AgvAction> GetSucceedingActionAsync(IAgv agv)
    {
        Logger.LogInformation("Starting succeeding action for Agv with CorrelationId: {AgvCorrelationId}", agv.CorrelationId);
        var subsequentAction = await _actionGroupRegistry.GetSubsequentActionAsync(agv.CorrelationId);
        return subsequentAction;
    }

    /// <summary>
    /// Creates the charging action for Agv and adds it to the registry
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    /// <exception cref="AgvGraphNotDefinedException"></exception>
    protected async Task<AgvAction?> CreateChargingActionAsync(IAgv agv)
    {
        if (IsAgvCharging(agv))
        {
            Logger.LogDebug("No need for a another charge action because the Agv is already charging for agvId: {AgvId}", agv.AgvId);
            // This might happen if the AssignAsync action is called several times due to a low SOC value, although the Agv is already charging
            return null;
        }
        Logger.LogInformation("Starting charge action for Agv with CorrelationId: {AgvCorrelationId}", agv.CorrelationId);

        var chargingNode = await _chargingPointManager.ReserveChargerAsync(agv);
        var action = await AddActionAsync(agv.CorrelationId, chargingNode, ActionType.Charge, creator: ActionGroupCreator.TransportOrder);
        return action;
    }

    /// <summary>
    /// Create maintenance action for Agv and adds it to the registry
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    /// <exception cref="AgvGraphNotDefinedException"></exception>
    protected async Task<AgvAction> CreateMaintenanceActionAsync(IAgv agv)
    {
        Logger.LogInformation("Starting maintenance action for Agv with CorrelationId: {AgvCorrelationId}", agv.CorrelationId);

        var maintenanceNode = await MaintenanceService.GetMaintenanceNode(agv);
        var action = await AddActionAsync(agv.CorrelationId, maintenanceNode, ActionType.Move, ActionGroupCreator.TransportOrder,"maintenance");

        return action;
    }

    /// <summary>
    /// Creates park action for Agv and adds it to the registry if not already on park node
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    protected async Task<AgvAction?> CreateParkActionAsync(IAgv agv)
    {
        if (await _parkPointService.IsOnAParkNodeAsync(agv))
        {
            Logger.LogDebug("No need for a park action because the Agv is already in park for agvId: {AgvCorrelationId}", agv.CorrelationId);
            return null;
        }
        Logger.LogInformation("Starting park action for Agv with CorrelationId: {AgvCorrelationId}", agv.CorrelationId);

        var parkNode = await _parkPointService.ReserveParkAsync(agv);
        var action = await AddActionAsync(agv.CorrelationId, parkNode, ActionType.Move, ActionGroupCreator.TransportOrder, "park");

        return action;
    }

    /// <summary>
    /// Gets the next action for Agv
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    protected async Task<AgvAction?> GetNotStartedGroupsFirstActionAsync(IAgv agv)
    {
        var actionGroup = await _chooseAgvActionGroupService.GetNotStartedActionGroupAsync(agv);
        return actionGroup?.Actions.OrderBy(a => a.Sequence).First();  
    } 

    /// <summary>
    /// Determine next action for agv
    /// </summary>
    /// <param name="agv">The Agv for which we want to get the next action</param>
    /// <returns></returns>
    protected abstract Task<AgvAction?> GetNextActionAsync(IAgv agv);

    /// <inheritdoc />
    public async Task AddActionGroupAsync(ActionGroup actionGroup, CancellationToken cancellationToken)
    {
        // this must be locked as well, because we are adding a new ActionGroup (external insert) and must ensure that this is done in serial. In this scenario the AssignAsync is used without lock!
        await ExecuteLockedAsync(async () =>
        {
            Logger.LogDebug("Adding ActionGroup with CorrelationId: {ActionGroupId}", actionGroup.CorrelationId);
            await AddActionGroupAsync(actionGroup);

            var agv = await GetReadyAgv(actionGroup, cancellationToken);

            if (agv != null)
            {
                Logger.LogInformation("Starting new ActionGroup directly for Agv {AgvId} with CorrelationId: {AgvCorrelationId}", agv.AgvId, agv.CorrelationId);
                await StartActionAsync(actionGroup.Actions.First(), agv, cancellationToken);
            }
            else
            {
                // no AGV found, will be assigned later
                Logger.LogDebug("No ready Agv was found directly for the ActionGroup {ActionGroupId}", actionGroup.CorrelationId);
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task CompleteActionAsync(Guid correlationId, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Completing action with CorrelationId: {ActionCorrelationId}", correlationId);
        var action = await _actionGroupRegistry.GetRequiredActionByIdAsync(correlationId);

        var actionGroup = await _actionGroupRegistry.GetRequiredActionGroupByIdAsync(action.ParentId);
        var agv = await GetAgvAsync(actionGroup.AssignedAgv, cancellationToken);

        await UpdateActionGroupAsync(action, agv, ActionState.Completed, cancellationToken);

        // charge will switch to charging state and can not be released immediately
        if (action.ActionType != ActionType.Charge)
        {
            Logger.LogDebug("Setting Agv with CorrelationId: {AgvCorrelationId} to idle, because action has been completed and type is not charge", agv.CorrelationId);

            await AgvStateService.SetIdleAsync(agv.CorrelationId, cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task AssignExclusivelyIfAgvReadyAsync(Guid agvCorrelationId, CancellationToken cancellationToken = default) => ExecuteLockedAsync(async () => { await AssignIfAgvReadyAsync(agvCorrelationId, cancellationToken); }, cancellationToken);

    /// <summary>
    /// Get manually triggered not started action for Agv
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    protected async Task<AgvAction?> GetManualActionAsync(IAgv agv)
    {
        IReadOnlyList<ActionGroup> actionGroups = await _actionGroupRegistry
            .GetActionGroupsAsync(ActionGroupState.Created, assignedAgvCorrelationId: agv.CorrelationId, isManual: true, maxItems: 1);

        var actionGroup = actionGroups.FirstOrDefault();
        if (actionGroup?.Actions.Count > 1)
        {
            throw new ArgumentException($"More than one actions in manual ActionGroup: {actionGroup}");
        }

        var action = actionGroup?.Actions.FirstOrDefault();
        return action;
    }

    /// <inheritdoc />
    public abstract Task CheckChargingAgvs(CancellationToken cancellationToken);
}