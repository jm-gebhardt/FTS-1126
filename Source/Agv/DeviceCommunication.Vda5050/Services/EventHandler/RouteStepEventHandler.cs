using Gebhardt.Acc.Common.Messages.Route;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Routes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.EventHandler;

/// <summary>
/// Handles route step execution related events.
/// </summary>
[UsedImplicitly]
public class RouteStepEventHandler(IRouteExecutionCoordinator coordinator, IAgvRepository agvRepository) : IConsumer<OrderStateReceived>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<OrderStateReceived> context)
    {
        var progress = await coordinator.DetermineRouteStepProgress(context.Message.CorrelationId, context.Message.OrderState);
        if (progress == null)
        {
            return;
        }
        await context.Publish(new RouteStepsProcessed(progress.RouteId, progress.AgvCorrelationId, [.. progress.ChangedSteps]));

        if (progress.OrderCompleted)
        {
            await ClearAgvCurrentOrder(progress.AgvCorrelationId, context.CancellationToken);
        }
    }

    private async Task ClearAgvCurrentOrder(Guid agvCorrelationId, CancellationToken cancellationToken)
    {
        var agv = await agvRepository.GetAgvAsync(agvCorrelationId);
        if (agv == null)
        {
            return;
        }
        agv.ClearOrder();
        await agvRepository.AddOrUpdateAsync(agv, cancellationToken);
    }
}
