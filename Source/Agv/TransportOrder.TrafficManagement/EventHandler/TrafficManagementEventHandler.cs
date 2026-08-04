using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.Route;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.EventHandler;

/// <summary>
/// Consumes incoming routing messages sequentially. The consumer registration in <see cref="Gebhardt.Acc.TransportOrder.TrafficManagement.Configurations.TrafficManagementBusRegistrationExtensions"/>
/// ensures that only one message is processed at a time. Consume all relevant messages for traffic management in this class to avoid concurrency issues
/// (or configure multiple consumers to use the same receive endpoint with a concurrency limit).
/// </summary>
[UsedImplicitly]
internal class TrafficManagementEventHandler(TrafficManagementTickState tickState, ILogger<TrafficManagementEventHandler> logger) : 
    IConsumer<StartActionCommand>,
    IConsumer<UpdateActionCommand>,
    IConsumer<AbortAndStartActionCommand>,
    IConsumer<CancelOrAbortActionGroup>,
    IConsumer<RouteStepsProcessed>,
    IConsumer<TrafficManagementTick>
{
    private static readonly TimeSpan _minimumTickInterval = TimeSpan.FromTicks(TrafficManagementTickPublisher.TickInterval.Ticks * 3 / 4);

    public Task Consume(ConsumeContext<StartActionCommand> context)
    {
        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<UpdateActionCommand> context)
    {
        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<AbortAndStartActionCommand> context)
    {
        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<CancelOrAbortActionGroup> context)
    {
        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<RouteStepsProcessed> context)
    {
        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<TrafficManagementTick> context)
    {
        if (!tickState.TryMarkProcessed(_minimumTickInterval))
        {
            return Task.CompletedTask; // discarded: a tick was already processed too recently
        }

        logger.LogInformation("Processing traffic management tick.");

        return Task.CompletedTask;
    }
}