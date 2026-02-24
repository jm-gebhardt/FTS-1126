using Gebhardt.Acc.TestPlanRunner.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TestPlanRunner.Services;

/// <summary>
/// Implementation of <see cref="ITestStepCancellationEvaluator"/>.
/// </summary>
public class TestStepCancellationEvaluator : ITestStepCancellationEvaluator
{
    private readonly ILogger<TestStepCancellationEvaluator> _logger;
    private readonly IAgvRouteRetrievalService _agvRouteRetrievalService;

    /// <summary>
    /// Minimum number of remaining nodes required to safely cancel a pick action (as it must not have started the physical pick process yet).
    /// </summary>
    private const int MinNodesForPickCancellation = 3;

    /// <inheritdoc cref="TestStepCancellationEvaluator"/>
    public TestStepCancellationEvaluator(
        ILogger<TestStepCancellationEvaluator> logger,
        IAgvRouteRetrievalService agvRouteRetrievalService)
    {
        _logger = logger;
        _agvRouteRetrievalService = agvRouteRetrievalService;
    }

    /// <inheritdoc />
    public async Task<bool> CanCancelAsync(Guid agvCorrelationId, StepType stepType)
    {
        switch (stepType)
        {
            case StepType.HostOrder:
                _logger.LogDebug("HostOrder step cannot be cancelled for AGV {AgvCorrelationId}", agvCorrelationId);
                return false;

            case StepType.Drop:
                _logger.LogDebug("Drop action cannot be cancelled for AGV {AgvCorrelationId}", agvCorrelationId);
                return false;

            case StepType.Move:
            case StepType.Charge:
                _logger.LogDebug("{StepType} action can always be cancelled for AGV {AgvCorrelationId}", stepType, agvCorrelationId);
                return true;

            case StepType.Pick:
                return await CanCancelPickAsync(agvCorrelationId);

            default:
                _logger.LogWarning("Unknown step type {StepType} for AGV {AgvCorrelationId}, probably not cancellable", stepType, agvCorrelationId);
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