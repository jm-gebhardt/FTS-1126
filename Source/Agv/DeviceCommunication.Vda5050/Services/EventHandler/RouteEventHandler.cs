using Gebhardt.Acc.Common.Messages.Route;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Routes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Order;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.EventHandler;

/// <summary>
/// Handles route-related messages.
/// </summary>
[UsedImplicitly]
public class RouteEventHandler(ILogger<RouteEventHandler> logger, IRouteExecutionCoordinator coordinator, IAgvRepository agvRepository) :
    IConsumer<RoutePlanned>,
    IConsumer<RouteUpdated>,
    IConsumer<RouteStepsReleased>,
    IConsumer<RouteAborted>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<RoutePlanned> context)
    {
        var message = context.Message;
        logger.LogInformation("Received {MessageType} for route {RouteId} of agv {AgvCorrelationId}", nameof(RoutePlanned), message.RouteId, message.AgvCorrelationId);
        var result = await coordinator.PlanRouteAsync(message.RouteId, message.AgvCorrelationId, message.RouteSteps);
        await Dispatch(context, result);
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<RouteUpdated> context)
    {
        var message = context.Message;
        logger.LogInformation("Received {MessageType} for route {RouteId} of agv {AgvCorrelationId}", nameof(RouteUpdated), message.RouteId, message.AgvCorrelationId);
        var result = await coordinator.UpdateRouteAsync(message.RouteId, message.RouteSteps);
        await Dispatch(context, result);
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<RouteStepsReleased> context)
    {
        var message = context.Message;
        logger.LogInformation("Received {MessageType} for route {RouteId} of agv {AgvCorrelationId}", nameof(RouteStepsReleased), message.RouteId, message.AgvCorrelationId);
        var result = await coordinator.ReleaseRouteStepsAsync(message.RouteId, message.RouteStepIds);
        await Dispatch(context, result);
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<RouteAborted> context)
    {
        var message = context.Message;
        logger.LogInformation("Received {MessageType} for route {RouteId} of agv {AgvCorrelationId}", nameof(RouteAborted), message.RouteId, message.AgvCorrelationId);
        var result = await coordinator.AbortRouteAsync(message.RouteId);
        await Dispatch(context, result);
    }

    private async Task Dispatch(ConsumeContext context, RouteCoordinationResult result)
    {
        switch (result)
        {
            case StartRouteOrder start:
                await SetAgvCurrentOrder(start.AgvCorrelationId, start.OrderId, start.RouteId, context.CancellationToken);
                logger.LogInformation("Starting order {OrderId} for route {RouteId} of agv {AgvCorrelationId}", start.OrderId, start.RouteId, start.AgvCorrelationId);
                await context.Publish(new StartOrder(start.OrderId, start.Order));
                break;

            case UpdateRouteOrder update:
                await SetAgvCurrentFleetManagerActionId(update.AgvCorrelationId, update.RouteId, context.CancellationToken);
                logger.LogInformation("Updating order {OrderId} for route {RouteId} of agv {AgvCorrelationId}", update.OrderId, update.RouteId, update.AgvCorrelationId);
                await context.Publish(new OrderUpdateRequested(update.OrderId, update.Order, update.RouteId));
                break;

            case CancelRouteOrder cancel:
                logger.LogInformation("Cancelling order {OrderId} for route {RouteId} of agv {AgvCorrelationId}", cancel.OrderId, cancel.RouteId, cancel.AgvCorrelationId);
                await context.Publish(new OrderCancellationRequested(cancel.OrderId));
                break;

            case RouteCoordinationFailed failed:
                logger.LogError("Route coordination failed: {Reason}", failed.Error.Description);
                break;

            case NoRouteAction:
                break;
        }
    }

    private async Task SetAgvCurrentOrder(Guid agvCorrelationId, Guid orderId, Guid routeId, CancellationToken cancellationToken)
    {
        var agv = await agvRepository.GetRequiredAgvAsync(agvCorrelationId);
        agv.CurrentOrder = orderId;
        agv.CurrentFleetManagerActionId = routeId;
        await agvRepository.AddOrUpdateAsync(agv, cancellationToken);
    }

    private async Task SetAgvCurrentFleetManagerActionId(Guid agvCorrelationId, Guid routeId, CancellationToken cancellationToken)
    {
        var agv = await agvRepository.GetRequiredAgvAsync(agvCorrelationId);
        agv.CurrentFleetManagerActionId = routeId;
        await agvRepository.AddOrUpdateAsync(agv, cancellationToken);
    }
}
