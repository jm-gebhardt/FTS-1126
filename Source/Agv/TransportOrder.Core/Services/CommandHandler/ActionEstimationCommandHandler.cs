using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using ActionGroup = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides handlers for ActionGroup estimation related commands.
/// </summary>
[UsedImplicitly]
public class ActionEstimationCommandHandler :
    IConsumer<EstimateActionGroupDuration>
{
    private readonly ILogger<ActionCommandHandler> _logger;
    private readonly IRouteService _routeService;
    private readonly IAgvRepository _agvRepository;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly IAgvDataRepository _agvDataRepository;

    /// <inheritdoc cref="ActionEstimationCommandHandler"/>
    public ActionEstimationCommandHandler(
        IRouteService routeService,
        IAgvRepository agvRepository,
        IActionGroupRepository actionGroupRepository,
        IAgvDataRepository agvDataRepository,
        ILogger<ActionCommandHandler> logger)
    {
        _logger = logger;
        _routeService = routeService;
        _agvRepository = agvRepository;
        _actionGroupRepository = actionGroupRepository;
        _agvDataRepository = agvDataRepository;
    }

    /// <inheritdoc cref="EstimateActionGroupDuration" />
    public async Task Consume(ConsumeContext<EstimateActionGroupDuration> context)
    {
        var action = context.Message.FirstAction;
        double duration = context.Message.FirstActionCosts;
        Task<ActionGroup> actionGroupTask = _actionGroupRepository.GetRequiredActionGroupByIdAsync(context.Message.ActionGroupId);
        Task<Domain.Models.Agv.Agv> agvTask = _agvRepository.GetAgvAsync(context.Message.AgvId);
        Task<AgvData?> agvDataTask = _agvDataRepository.GetAgvDataAsync(context.Message.AgvId);
        await Task.WhenAll(actionGroupTask, agvTask, agvDataTask);
        var actionGroup = actionGroupTask.Result;
        var assignedAgv = agvTask.Result;
        var agvData = agvDataTask.Result;

        List<AgvAction> actionsAfterFirstAction = actionGroup.Actions.Where(a => a.Sequence > action.Sequence).OrderBy(a => a.Sequence).ToList();
      
        _logger.LogDebug("First action costs: {FirstActionCosts}", duration);
        if (actionsAfterFirstAction.Any())
        {
            var followingRoute = await _routeService.GetRouteAsync(assignedAgv, actionsAfterFirstAction.Select(a => a.TargetNodeId).ToList(), action.TargetNodeId, agvData);
            _logger.LogDebug("Following route costs: {FollowingRouteCosts}", followingRoute.Costs);
            duration += followingRoute.Costs;
            // costs for charging itself, just assume 1% / s, we don't have the charging rate here
            double chargeCostsPerCharge = (100 - assignedAgv.AgvStatus!.BatteryCharge) * 1;
            double chargeCosts = actionsAfterFirstAction.Count(a => a.ActionType == ActionType.Charge) * chargeCostsPerCharge;
            _logger.LogDebug("Charging costs: {ChargeCosts}", chargeCosts);
            duration += chargeCosts;

            // costs for action switching / state transitions of the Agv from one action to the next action
            int actionSwitchingCosts = actionsAfterFirstAction.Count * 2;
            _logger.LogDebug("Action switching costs: {ActionSwitchingCosts}", actionSwitchingCosts);
            duration += actionSwitchingCosts;
        }

        actionGroup.EstimatedDuration = TimeSpan.FromSeconds(duration);
        _logger.LogDebug("Estimated duration for action group {ActionGroupId} and agv {AgvId}: {EstimatedDuration}", actionGroup.CorrelationId, assignedAgv.CorrelationId, actionGroup.EstimatedDuration);
        await _actionGroupRepository.UpdateAsync(actionGroup);
    }
}