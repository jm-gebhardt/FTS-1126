using Gebhardt.Acc.Common.Messages.Route.RouteSteps;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Routes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Routes;

/// <inheritdoc cref="IRouteExecutionCoordinator"/>
public class RouteExecutionCoordinator(
    IRouteExecutionRepository routeExecutionRepository,
    IAgvRepository agvRepository,
    IRouteStepTranslator routeStepTranslator) : IRouteExecutionCoordinator
{
    /// <inheritdoc/>
    public async Task<RouteCoordinationResult> PlanRouteAsync(Guid routeId, Guid agvCorrelationId, RouteStep[] routeSteps)
    {
        var routeExecution = await routeExecutionRepository.GetAsync(routeId);
        if (routeExecution == null)
        {
            routeExecution = new RouteExecution(routeId, agvCorrelationId);
            routeExecution.ApplyPlanned(routeSteps.ToStepExecutions());
            await routeExecutionRepository.AddAsync(routeExecution);
            return new NoRouteAction();
        }
        routeExecution.ApplyPlanned(routeSteps.ToStepExecutions());
        await routeExecutionRepository.UpdateAsync(routeExecution);
        return new NoRouteAction();
    }

    /// <inheritdoc/>
    public async Task<RouteCoordinationResult> UpdateRouteAsync(Guid routeId, RouteStep[] routeSteps)
    {
        var routeExecution = await routeExecutionRepository.GetRequiredAsync(routeId);
        routeExecution.ApplyUpdate(routeSteps.ToStepExecutions());

        if (routeExecution.CurrentOrderId == null)
        {
            await routeExecutionRepository.UpdateAsync(routeExecution);
            return new NoRouteAction();
        }

        if (routeExecution.CurrentOrderCompleted)
        {
            var restartedOrderId = routeExecution.MintNextOrderId();
            var restartedRoute = await routeStepTranslator.Translate(routeExecution.AgvCorrelationId, [.. routeExecution.Steps]);
            await routeExecutionRepository.UpdateAsync(routeExecution);
            var restartAgv = await agvRepository.GetRequiredAgvAsync(routeExecution.AgvCorrelationId);
            return new StartRouteOrder(routeExecution.RouteId, routeExecution.AgvCorrelationId, restartedOrderId, new Models.Orders.Order(restartAgv, restartedOrderId, restartedRoute));
        }

        var updatedRoute = await routeStepTranslator.Translate(routeExecution.AgvCorrelationId, [.. routeExecution.Steps]);
        await routeExecutionRepository.UpdateAsync(routeExecution);
        var agv = await agvRepository.GetRequiredAgvAsync(routeExecution.AgvCorrelationId);
        return new UpdateRouteOrder(routeExecution.RouteId, routeExecution.AgvCorrelationId, routeExecution.CurrentOrderId.Value, new Models.Orders.Order(agv, routeExecution.CurrentOrderId.Value, updatedRoute));
    }

    /// <inheritdoc/>
    public async Task<RouteCoordinationResult> ReleaseRouteStepsAsync(Guid routeId, Guid[] routeStepIds)
    {
        var routeExecution = await routeExecutionRepository.GetRequiredAsync(routeId);
        var releaseResult = routeExecution.ReleaseSteps(routeStepIds);
        if (!releaseResult.IsSuccess)
        {
            return new RouteCoordinationFailed(releaseResult.Error!);
        }

        var agv = await agvRepository.GetRequiredAgvAsync(routeExecution.AgvCorrelationId);
        var route = await routeStepTranslator.Translate(routeExecution.AgvCorrelationId, [.. routeExecution.Steps]);

        if (routeExecution.CurrentOrderId == null)
        {
            var newOrderId = routeExecution.MintNextOrderId();
            await routeExecutionRepository.UpdateAsync(routeExecution);
            return new StartRouteOrder(routeExecution.RouteId, routeExecution.AgvCorrelationId, newOrderId, new Models.Orders.Order(agv, newOrderId, route));
        }

        await routeExecutionRepository.UpdateAsync(routeExecution);
        return new UpdateRouteOrder(routeExecution.RouteId, routeExecution.AgvCorrelationId, routeExecution.CurrentOrderId.Value, new Models.Orders.Order(agv, routeExecution.CurrentOrderId.Value, route));
    }

    /// <inheritdoc/>
    public async Task<RouteCoordinationResult> AbortRouteAsync(Guid routeId)
    {
        var routeExecution = await routeExecutionRepository.GetRequiredAsync(routeId);
        var currentOrderId = routeExecution.CurrentOrderId;
        routeExecution.Abort();
        await routeExecutionRepository.UpdateAsync(routeExecution);

        if (currentOrderId == null)
        {
            return new NoRouteAction();
        }
        return new CancelRouteOrder(routeExecution.RouteId, routeExecution.AgvCorrelationId, currentOrderId.Value);
    }

    /// <inheritdoc/>
    public async Task<RouteStepProgress?> DetermineRouteStepProgress(Guid orderId, OrderState orderState)
    {
        var routeExecution = await routeExecutionRepository.GetByCurrentOrderIdAsync(orderId);
        if (routeExecution == null)
        {
            return null;
        }

        List<RouteStepState> changedStates = [];
        foreach (var step in routeExecution.Steps)
        {
            var status = step.DetermineStatus(orderState);
            if (status == step.Status)
            {
                continue;
            }
            string? failureReason = status == RouteStepStatus.Failed ? step.ExtractFailureReason(orderState) : null;
            step.RecordStatus(status, failureReason);
            changedStates.Add(new RouteStepState(step.RouteStepId, status, failureReason));
        }

        if (routeExecution.Steps.Count > 0 && routeExecution.Steps.All(s => s.Status == RouteStepStatus.Completed))
        {
            routeExecution.CurrentOrderCompleted = true;
        }

        await routeExecutionRepository.UpdateAsync(routeExecution);

        if (changedStates.Count == 0)
        {
            return null;
        }
        return new RouteStepProgress(routeExecution.RouteId, routeExecution.AgvCorrelationId, changedStates, routeExecution.CurrentOrderCompleted);
    }
}
