using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TestPlanRunner.Services;

/// <summary>
/// Implementation of <see cref="IActionCancellationEvaluator"/>.
/// </summary>
public class ActionCancellationEvaluator : IActionCancellationEvaluator
{
    private readonly ILogger<ActionCancellationEvaluator> _logger;
    private readonly IAgvRouteRetrievalService _agvRouteRetrievalService;

    /// <summary>
    /// Minimum number of remaining nodes required to safely cancel a pick action (as it must not have started the physical pick process yet).
    /// </summary>
    private const int MinNodesForPickCancellation = 3;

    /// <inheritdoc cref="ActionCancellationEvaluator"/>
    public ActionCancellationEvaluator(
        ILogger<ActionCancellationEvaluator> logger,
        IAgvRouteRetrievalService agvRouteRetrievalService)
    {
        _logger = logger;
        _agvRouteRetrievalService = agvRouteRetrievalService;
    }

    /// <inheritdoc />
    public async Task<bool> CanCancelAsync(Guid agvCorrelationId, ActionType actionType)
    {
        switch (actionType)
        {
            case ActionType.Drop:
                _logger.LogDebug("Drop action cannot be cancelled for AGV {AgvCorrelationId}", agvCorrelationId);
                return false;

            case ActionType.Move:
            case ActionType.Charge:
                _logger.LogDebug("{ActionType} action can always be cancelled for AGV {AgvCorrelationId}", actionType, agvCorrelationId);
                return true;

            case ActionType.Pick:
                return await CanCancelPickAsync(agvCorrelationId);

            default:
                _logger.LogWarning("Unknown action type {ActionType} for AGV {AgvCorrelationId}, probably not cancellable", actionType, agvCorrelationId);
                return false;
        }
    }

    private async Task<bool> CanCancelPickAsync(Guid agvCorrelationId)
    {
        try
        {
            var agvRoute = await _agvRouteRetrievalService.GetAgvRouteAsync(agvCorrelationId);

            if (agvRoute == null)
            {
                _logger.LogDebug("No route found for AGV {AgvCorrelationId}, pick action cannot be cancelled", agvCorrelationId);
                return false;
            }

            int remainingNodes = agvRoute.RemainingNodes.Length;
            bool canCancel = remainingNodes >= MinNodesForPickCancellation;

            _logger.LogDebug(
                "Pick action for AGV {AgvCorrelationId}: {RemainingNodes} remaining nodes, cancellable: {CanCancel}",
                agvCorrelationId, remainingNodes, canCancel);

            return canCancel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking route for AGV {AgvCorrelationId}, assuming pick action can not be cancelled", agvCorrelationId);
            return false;
        }
    }
}