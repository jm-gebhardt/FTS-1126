using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.SagaStateMachines;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using TransportOrder.Domain.Repositories;
using TransportOrder.Domain.SemaphoreProvider;
using TransportOrder.Domain.Services.ActionGroup;
using TransportOrder.Domain.Services.Routing;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// 
/// </summary>
public class ActionManager : IActionManager
{
    private readonly IActionManagerSemaphoreProvider _semaphoreProvider;
    private readonly IBus _bus;
    private readonly IRouteService _routeService;
    private readonly IChooseAgvActionGroupService _chooseAgvActionGroupService;
    private readonly IStartActionService _startActionService;
    private readonly ILogger<ActionManager> _logger;
    private readonly IChargingService _chargingService;
    private readonly IAgvSagaCommunicationService _agvSagaCommunicationService;
    private readonly IActionGroupRegistry _actionGroupRegistry;


    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="bus">Abstracted interface for Agv action registry</param>
    /// <param name="actionGroupRegistry">Abstracted interface for Agv action registry</param>
    /// <param name="routeService">Service for computation of Route for Agv</param>
    /// <param name="logger">generic interface for logging</param>
    /// <param name="chargingService"></param>
    /// <param name="agvSagaCommunicationService"></param>
    /// <param name="startActionService"></param>
    /// <param name="chooseAgvActionGroupService"></param>
    /// <param name="semaphoreProvider"></param>
    public ActionManager(
        IBus bus,
        IActionGroupRegistry actionGroupRegistry,
        IRouteService routeService,
        ILogger<ActionManager> logger,
        IChargingService chargingService,
        IAgvSagaCommunicationService agvSagaCommunicationService,
        IStartActionService startActionService,
        IChooseAgvActionGroupService chooseAgvActionGroupService,
        IActionManagerSemaphoreProvider semaphoreProvider)
    {
        _bus = bus;
        _actionGroupRegistry = actionGroupRegistry;
        _routeService = routeService;
        _logger = logger;
        _chargingService = chargingService;
        _agvSagaCommunicationService = agvSagaCommunicationService;
        _chooseAgvActionGroupService = chooseAgvActionGroupService;
        _startActionService = startActionService;
        _semaphoreProvider = semaphoreProvider;
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

    private Task<IAgv> GetAgvAsync(Guid? correlationId, CancellationToken cancellationToken) => _agvSagaCommunicationService.GetAgvAsync(correlationId, cancellationToken);

    /// <summary>
    /// Chooses the best Agv available (which is confirmed to be ready) for the given <see cref="ActionGroup"/>
    /// </summary>
    /// <param name="actionGroup"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<IAgv?> GetReadyAgv(ActionGroup actionGroup, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ready Agv for ActionGroup with CorrelationId: {ActionGroupId}", actionGroup.CorrelationId);

        Guid? assignedAgv = actionGroup.AssignedAgv;

        if (assignedAgv != null)
        {
            _logger.LogDebug("The Agv is already assigned ActionGroup with CorrelationId: {ActionGroupId}, so verify it can be set to busy. The ActionGroup is Manual {IsManual}", actionGroup.CorrelationId, actionGroup.IsManual);

            // already assigned (e.g. manual ActionGroup)
            // we ignore the IsLocked for the manual ActionGroup
            var agvCorrelationId = (Guid) assignedAgv;
            return await _agvSagaCommunicationService.IsAgvReadyForBusyAsync(agvCorrelationId, cancellationToken, ignoreCharging: true, ignoreLocked: actionGroup.IsManual) ? await GetAgvAsync(agvCorrelationId, cancellationToken) : null;
        }

        List<IAgv> agvs = await GetReadyAndRecruitableAgvsAsync(cancellationToken);
        var agv = await _chooseAgvActionGroupService.GetAgvAsync(actionGroup, agvs);
        if (agv == null)
        {
            return null;
        }
        _logger.LogDebug("Found Agv for ActionGroup with CorrelationId: {ActionGroupId}", actionGroup.CorrelationId);

        return await GetAgvAsync(agv.CorrelationId, cancellationToken);
    }

    private async Task<List<IAgv>> GetReadyAndRecruitableAgvsAsync(CancellationToken cancellationToken)
    {
        // we take all ready Agvs
        Task<IEnumerable<IAgv>> readyAgvsTask = _agvSagaCommunicationService.GetReadyAgvsAsync(false, cancellationToken);

        List<IAgv> chargingAgvsWithGoodBatteryLevel = await _chargingService.GetChargingAgvsWithGoodBatteryLevel();
        // + recruitable  (charging) Agvs
        Task<IEnumerable<IAgv>> recruitableChargingAgvsTasks = _agvSagaCommunicationService.GetReadyAgvsAsync(chargingAgvsWithGoodBatteryLevel, true, cancellationToken);

        IList<IAgv> normalReadyAgvs = (await readyAgvsTask).ToList();
        IList<IAgv> recruitableChargingAgvs = (await recruitableChargingAgvsTasks).ToList();
        _logger.LogDebug("Choosing Agv for ActionGroup out of: {NormalReadyAgvsCount} normal ready Agvs and {RecruitableChargingAgvsCount} recruitable charging Agvs", normalReadyAgvs.Count, recruitableChargingAgvs.Count);

        List<IAgv> agvs = normalReadyAgvs.Concat(recruitableChargingAgvs).ToList();
        return agvs;
    }

    /// <summary>
    /// Send <see cref="StartActionCommand"/> and set action to start>.
    /// </summary>
    [PublicAPI]
    public async Task StartActionAsync(AgvAction action, IAgv assignedAgv, CancellationToken cancellationToken)
    {
        action.Nodes = (await _routeService.GetRouteAsync(assignedAgv, action.TargetNodeId)).Nodes;

        _logger.LogDebug("Starting action with CorrelationId: {ActionCorrelationId} for Agv {AgvCorrelationId}. Setting Agv to busy in next step.", action.CorrelationId, assignedAgv.CorrelationId);
        await _agvSagaCommunicationService.SetBusyAsync(assignedAgv, true, true, cancellationToken);

        try
        {
            await _startActionService.StartAction(assignedAgv, action, cancellationToken);
            _logger.LogDebug("Action with CorrelationId: {ActionCorrelationId} for Agv {AgvCorrelationId} has been started. Updating ActionGroup.State to Started in next step.", action.CorrelationId, assignedAgv.CorrelationId);
            await UpdateActionGroupAsync(action, assignedAgv, ActionState.Started, cancellationToken);

            _logger.LogTrace("Starting action done");
        }
        catch
        {
            // release Agv in case of error
            await _agvSagaCommunicationService.SetIdleAsync(assignedAgv.CorrelationId, cancellationToken);
            throw;
        }
    }
   
    /// <inheritdoc />
    public async Task AddActionGroupAsync(ActionGroup actionGroup, CancellationToken cancellationToken)
    {
        // this must be locked as well, because we are adding a new ActionGroup (external insert) and must ensure that this is done in serial. In this scenario the AssignAsync is used without lock!
        await _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            _logger.LogDebug("Adding ActionGroup with CorrelationId: {ActionGroupId}", actionGroup.CorrelationId);
            await AddActionGroupAsync(actionGroup);

            var agv = await GetReadyAgv(actionGroup, cancellationToken);

            if (agv != null)
            {
                _logger.LogInformation("Starting new ActionGroup directly for Agv {AgvId} with CorrelationId: {AgvCorrelationId}", agv.AgvId, agv.CorrelationId);
                await StartActionAsync(actionGroup.Actions.First(), agv, cancellationToken);
            }
            else
            {
                // no AGV found, will be assigned later
                _logger.LogDebug("No ready Agv was found directly for the ActionGroup {ActionGroupId}", actionGroup.CorrelationId);
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task CompleteActionAsync(Guid correlationId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Completing action with CorrelationId: {ActionCorrelationId}", correlationId);
        var action = await _actionGroupRegistry.GetRequiredActionByIdAsync(correlationId);

        var actionGroup = await _actionGroupRegistry.GetRequiredActionGroupByIdAsync(action.ParentId);
        var agv = await GetAgvAsync(actionGroup.AssignedAgv, cancellationToken);

        await UpdateActionGroupAsync(action, agv, ActionState.Completed, cancellationToken);

        // charge will switch to charging state and can not be released immediately
        if (action.ActionType != ActionType.Charge)
        {
            _logger.LogDebug("Setting Agv with CorrelationId: {AgvCorrelationId} to idle, because action has been completed and type is not charge", agv.CorrelationId);

            await _agvSagaCommunicationService.SetIdleAsync(agv.CorrelationId, cancellationToken);
        }
    }
}