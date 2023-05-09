using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Charging;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Choosers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Maintenance;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Parking;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;

/// <summary>
/// Base class for ActionManager which provides implementations for creating methods (Pick, Drop, Charge and Maintenance) and all base logic of all ActionManagers
/// </summary>
public abstract class BaseActionManager : IActionManager
{
    private readonly SemaphoreSlim _lockSemaphoreSlim = new(1, 1);

    private readonly IAgvActionRegistry _agvActionRegistry;
    private readonly IRouteService _routeService;
    private readonly IChargingPointManager _chargingPointManager;
    private readonly IParkPointService _parkPointService;
    private readonly IAgvRegistry _agvRegistry;
    private readonly IAgvService _agvService;
    private readonly IAgvStateService _agvStateService;
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
    /// Constructor
    /// </summary>
    /// <param name="agvActionRegistry">Abstracted interface for Agv action registry</param>
    /// <param name="routeService">Service for computation of Route for Agv</param>
    /// <param name="chargingPointManager">Finds and Reserves charging node for agv</param>
    /// <param name="parkPointService"></param>
    /// <param name="maintenanceService">Providing information about maintenance requests for Agvs</param>
    /// <param name="logger">generic interface for logging</param>
    /// <param name="chargingService"></param>
    /// <param name="agvService"></param>
    /// <param name="agvLoadCapabilityService"></param>
    /// <param name="startActionService"></param>
    /// <param name="agvRegistry">Interface for accessing the AGV registry</param>
    /// <param name="agvStateService"></param>
    /// <param name="chooseAgvActionGroupService"></param>
    protected BaseActionManager(
        IAgvActionRegistry agvActionRegistry,
        IRouteService routeService,
        IChargingPointManager chargingPointManager,
        IParkPointService parkPointService,
        IMaintenanceService maintenanceService,
        ILogger logger,
        IChargingService chargingService,
        IAgvRegistry agvRegistry,
        IAgvService agvService,
        IAgvLoadCapabilityService agvLoadCapabilityService,
        IStartActionService startActionService,
        IAgvStateService agvStateService,
        IChooseAgvActionGroupService chooseAgvActionGroupService)
    {
        _agvActionRegistry = agvActionRegistry;
        _routeService = routeService;
        _chargingPointManager = chargingPointManager;
        _parkPointService = parkPointService;
        MaintenanceService = maintenanceService;
        Logger = logger;
        ChargingService = chargingService;
        _agvRegistry = agvRegistry;
        _agvService = agvService;
        AgvLoadCapabilityService = agvLoadCapabilityService;
        _agvStateService = agvStateService;
        _chooseAgvActionGroupService = chooseAgvActionGroupService;
        _startActionService = startActionService;
    }

    private static AgvAction CreateAgvAction(IAgv agv, AgvGraph.Node node, ActionType actionType = ActionType.Move)
    {
        return new AgvAction(
            Guid.NewGuid(),
            agv.AgvId,
            actionType,
            string.Empty,
            Guid.Empty,
            node.Id);
    }

    /// <inheritdoc />
    public async Task AddActionGroupAsync(ActionGroup actionGroup, CancellationToken cancellationToken)
    {
        await _lockSemaphoreSlim.WaitAsync(cancellationToken);

        try
        {
            foreach (var action in actionGroup.Actions)
            {
                await AddAgvActionAsync(action);
            }

            var agv = await ChooseAgv(actionGroup);

            if (agv != null)
            {
                Logger.LogInformation("Starting new ActionGroup directly for Agv with CorrelationId: {AgvCorrelationId}", agv.CorrelationId);
                await AssignAsync(agv, true, cancellationToken);
            }
            else
            {
                // no AGV found, will be assigned later
                Logger.LogDebug("No AGV was directly found for action group {ActionGroupId}", actionGroup.CorrelationId);
            }
        }
        finally
        {
            _lockSemaphoreSlim.Release();
        }
    }

    /// <summary>
    /// Chooses the best Agv available for the given <see cref="ActionGroup"/>
    /// </summary>
    /// <param name="actionGroup"></param>
    /// <returns></returns>
    private async Task<IAgv?> ChooseAgv(ActionGroup actionGroup)
    {
        // we take all ready Agvs
        Task<IEnumerable<IAgv>> readyAgvsTask = _agvService.GetReadyAgvsAsync(false);

        List<IAgv> chargingAgvsWithGoodBatteryLevel = ChargingService.GetChargingAgvsWithGoodBatteryLevel();
        // + recruitable  (charging) Agvs
        Task<IEnumerable<IAgv>> recruitableChargingAgvsTasks = _agvService.GetReadyAgvsAsync(chargingAgvsWithGoodBatteryLevel, true);

        IList<IAgv> normalReadyAgvs = (await readyAgvsTask).ToList();
        IList<IAgv> recruitableChargingAgvs = (await recruitableChargingAgvsTasks).ToList();
        Logger.LogDebug("Choosing Agv for ActionGroup out of: {NormalReadyAgvsCount} normal ready Agvs and {RecruitableChargingAgvsCount} recruitable charging Agvs", normalReadyAgvs.Count, recruitableChargingAgvs.Count);

        List<IAgv> agvs = normalReadyAgvs.Concat(recruitableChargingAgvs).ToList();
        return await _chooseAgvActionGroupService.GetAgvAsync(actionGroup, agvs);
    }

    /// <summary>
    /// Send <see cref="StartActionCommand"/> and set action to start>.
    /// </summary>
    [PublicAPI]
    protected async Task StartActionAsync(AgvAction action, IAgv assignedAgv, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Starting action with CorrelationId: {ActionCorrelationId}", action.CorrelationId);
        string[] nodes = _routeService.GetRoute(assignedAgv, action.TargetNodeId).Nodes;
        action.AssignedAgv = assignedAgv.CorrelationId;
        action.AgvId = assignedAgv.AgvId;
        action.Nodes = nodes;
        await _agvActionRegistry.SetActionStartedAsync(action);

        await _agvStateService.SetBusyAsync(assignedAgv, true);
        await _startActionService.StartAction(action, cancellationToken);
        Logger.LogTrace("Starting action done");
    }

    /// <inheritdoc />
    public async Task CompleteActionAsync(Guid correlationId, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Completing action with CorrelationId: {ActionCorrelationId}", correlationId);
        var action = await GetAgvActionAsync(correlationId);
        var agv = await GetAgvByCorrelationIdAsync(action.AssignedAgv);

        await UpdateActionStateAsync(action, ActionState.Completed);

        // charge will switch to charging state and can not be released immediately 
        if (action.ActionType != ActionType.Charge)
        {
            Logger.LogDebug("Setting Agv with CorrelationId: {AgvCorrelationId}, because action has been completed and type is not charge", agv.CorrelationId);

            await _agvStateService.SetIdleAsync(agv);
        }
    }

    /// <summary>
    /// Get agv action by correlation Id for Action Event consumption
    /// </summary>
    /// <param name="correlationId"></param>
    /// <returns></returns>
    /// <exception cref="ActionNotFoundException"></exception>
    private Task<AgvAction> GetAgvActionAsync(Guid correlationId) => Task.FromResult(_agvActionRegistry.GetRequiredActionById(correlationId));

    /// <summary>
    /// Gets agv by correlation id
    /// </summary>
    /// <param name="correlationId"></param>
    /// <returns></returns>
    /// <exception cref="AgvNotFoundException"></exception>
    private Task<IAgv> GetAgvByCorrelationIdAsync(Guid? correlationId) => Task.FromResult(_agvRegistry.GetRequiredAgv(correlationId));

    /// <summary>
    /// Updates state of Action
    /// </summary>
    /// <param name="action">The action to update</param>
    /// <param name="actionState">The target state</param>
    /// <returns></returns>
    private Task UpdateActionStateAsync(AgvAction action, ActionState actionState)
    {
        _agvActionRegistry.UpdateState(action.CorrelationId, actionState);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Saves Agv Action to registry
    /// </summary>
    /// <param name="agvAction"></param>
    private Task AddAgvActionAsync(AgvAction agvAction)
    {
        _agvActionRegistry.Insert(agvAction);
        return Task.CompletedTask;
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
        var subsequentAction = await _agvActionRegistry.GetSubsequentActionAsync(agv.CorrelationId);
        return subsequentAction;
    }

    /// <summary>
    /// Gets the charging action for Agv
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    /// <exception cref="AgvGraphNotDefinedException"></exception>
    protected async Task<AgvAction?> CreateChargingActionAsync(IAgv agv)
    {
        if (IsAgvCharging(agv))
        {
            Logger.LogDebug("No need for a another charge action because the Agv is already charging for agvId: {AgvId}", agv.AgvId);
            // This might happen if the AssignAsync action is called several times due to a low SOC value, although the Agv is already loaded
            return null;
        }
        Logger.LogInformation("Starting charge action for Agv with CorrelationId: {AgvCorrelationId}", agv.CorrelationId);

        var chargingNode = await _chargingPointManager.ReserveChargerAsync(agv);
        return CreateAgvAction(agv, chargingNode, ActionType.Charge);
    }

    private bool IsAgvCharging(IAgv agv) => agv.GetRequiredAgvStatus().Charging;

    /// <summary>
    /// Gets maintenance action for Agv
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    /// <exception cref="AgvGraphNotDefinedException"></exception>
    protected Task<AgvAction> CreateMaintenanceActionAsync(IAgv agv)
    {
        Logger.LogInformation("Starting maintenance action for Agv with CorrelationId: {AgvCorrelationId}", agv.CorrelationId);

        var maintenanceNode = MaintenanceService.GetMaintenanceNode(agv);
        return Task.FromResult(CreateAgvAction(agv, maintenanceNode));
    }

    /// <summary>
    /// Gets park action for Agv if not already on park node
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
        return CreateAgvAction(agv, parkNode);
    }

    /// <summary>
    /// Gets the next action for Agv
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    protected Task<AgvAction?> GetNextActionGroupAsync(IAgv agv) => _chooseAgvActionGroupService.GetActionGroupAsync(agv);

    /// <inheritdoc />
    public async Task AssignExclusivelyAsync(IAgv agv, bool ignoreChargingFlag = false, CancellationToken cancellationToken = default)
    {
        await _lockSemaphoreSlim.WaitAsync(cancellationToken);

        try
        {
            await AssignAsync(agv, ignoreChargingFlag, cancellationToken);
        }
        finally
        {
            _lockSemaphoreSlim.Release();
        }
    }

    /// <summary>
    /// Assigns a new action for Agv, but it does not use the semaphore
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="ignoreChargingFlag">If true the return value will not take the charging flag of the Agv into account</param>
    /// <param name="cancellationToken"></param>
    private async Task AssignAsync(IAgv agv, bool ignoreChargingFlag, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Assigning action for Agv with CorrelationId: {AgvCorrelationId}", agv.CorrelationId);

        // set agv to busy, to lock it exclusively
        if (!await _agvService.IsAgvReadyForBusyAsync(agv, ignoreChargingFlag))
        {
            // Agv might be charging
            Logger.LogDebug("Agv with CorrelationId {AgvCorrelationId} is not ready to be set to busy", agv.CorrelationId);
            return;
        }

        // Work on pending actions
        // or if there is nothing pending go either charge if possible or park
        var nextAgvAction = await GetNextActionAsync(agv);

        if (nextAgvAction != null)
        {
            await StartActionAsync(nextAgvAction, agv, cancellationToken);
        }
        else
        {
            Logger.LogDebug("There is no next action available for Agv with CorrelationId {AgvCorrelationId}", agv.CorrelationId);
        }
    }

    /// <summary>
    /// Determine next action for agv
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    protected abstract Task<AgvAction?> GetNextActionAsync(IAgv agv);

    /// <inheritdoc />
    public abstract Task CheckChargingAgvs(CancellationToken cancellationToken);
}