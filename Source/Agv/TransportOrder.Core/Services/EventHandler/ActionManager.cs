using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.SagaStateMachines;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using ActionGroup = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Provides a service for actions.
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
    private readonly IMapper _mapper;
    private readonly IActionGroupValidationService _actionGroupValidationService;

    /// <summary>
    /// Constructor
    /// </summary>
    public ActionManager(
        IBus bus,
        IActionGroupRegistry actionGroupRegistry,
        IRouteService routeService,
        ILogger<ActionManager> logger,
        IChargingService chargingService,
        IAgvSagaCommunicationService agvSagaCommunicationService,
        IStartActionService startActionService,
        IChooseAgvActionGroupService chooseAgvActionGroupService,
        IActionManagerSemaphoreProvider semaphoreProvider,
        IActionGroupValidationService actionGroupValidationService,
        IMapper mapper)
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
        _mapper = mapper;
        _actionGroupValidationService = actionGroupValidationService;
    }

    private async Task AddActionGroupAsync(ActionGroup actionGroup)
    {
        await _actionGroupRegistry.AddAsync(actionGroup);
        await _bus.Publish(new ActionGroupCreated(_mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), actionGroup.CorrelationId));
    }

    private async Task UpdateActionGroupAsync(AgvAction action, IAgv agv, ActionState state, CancellationToken cancellationToken)
    {
        var actionGroup = await _actionGroupRegistry.UpdateActionAndGroupStateAsync(action, agv, state);
        await _bus.Publish(new ActionGroupChanged(_mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), actionGroup.CorrelationId), cancellationToken);
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
        var agv = await _chooseAgvActionGroupService.GetAgvAsync(actionGroup, agvs, cancellationToken);
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
    /// Send <see cref="Common.Messages.ActionGroups.Commands.StartActionCommand"/> and set action to start>.
    /// </summary>
    [PublicAPI]
    public async Task StartActionAsync(AgvAction action, IAgv assignedAgv, CancellationToken cancellationToken)
    {
        action.Nodes = (await _routeService.GetRouteAsync(assignedAgv, action.TargetNodeId, cancellationToken)).Nodes;

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
        _logger.LogTrace("Validating ActionGroup with CorrelationId: {ActionGroupId}", actionGroup.CorrelationId);
        IEnumerable<ValidationFailure> validationFailures = await _actionGroupValidationService.ValidateAsync(actionGroup);

        if (validationFailures.Any())
        {
            await HandleValidationFailure(actionGroup, validationFailures, cancellationToken);
            return;
        }
        // this must be locked as well, because we are adding a new ActionGroup (external insert) and must ensure that this is done in serial. In this scenario the AssignAsync is used without lock!
        await _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            _logger.LogDebug("Adding ActionGroup with CorrelationId: {ActionGroupId}", actionGroup.CorrelationId);
            await AddActionGroupAsync(actionGroup);

            var agv = await GetReadyAgv(actionGroup, cancellationToken);

            if (agv != null)
            {
                var action = actionGroup.Actions.OrderBy(a => a.Sequence).First();

                _logger.LogInformation("Starting new ActionGroup directly for Agv {AgvId} with CorrelationId: {AgvCorrelationId}, ActionType: {ActionType}", agv.AgvId, agv.CorrelationId, Enum.GetName(action.ActionType));

                await StartActionAsync(action, agv, cancellationToken);
            }
            else
            {
                // no AGV found, will be assigned later
                _logger.LogDebug("No ready Agv was found directly for the ActionGroup {ActionGroupId}", actionGroup.CorrelationId);
            }
        }, cancellationToken);
    }

    private async Task HandleValidationFailure(ActionGroup actionGroup, IEnumerable<ValidationFailure> validationFailures, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Validation result of ActionGroup with CorrelationId: {ActionGroupId} is not valid!", actionGroup.CorrelationId);

        if (actionGroup.IsManual)
        {
            // in case of a manual action we do not want to add it to our registry and the caller is responsible for a proper exception handling
            throw new ValidationException(validationFailures);
        }
        actionGroup.InvalidateActionGroup();
        _logger.LogDebug("Adding invalid ActionGroup with CorrelationId: {ActionGroupId}", actionGroup.CorrelationId);
        await AddActionGroupAsync(actionGroup, cancellationToken);

        _logger.LogDebug("ActionGroup with CorrelationId: {ActionGroupId} is not valid, publishing ActionGroupInvalid and return", actionGroup.CorrelationId);
        List<string> validationFailureReasons = validationFailures.Select(error => error.ErrorMessage).ToList();

        await _bus.Publish(new ActionGroupInvalid(actionGroup.CorrelationId, _mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), validationFailureReasons), cancellationToken);
    }
}