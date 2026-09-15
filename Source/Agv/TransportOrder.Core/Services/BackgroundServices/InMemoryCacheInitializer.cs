using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;

/// <summary>
/// Provides a service to initialize the in memory caches.
/// </summary>
[UsedImplicitly]
public class InMemoryCacheInitializer(
    ITriggerRepository triggerRepository,
    ICargoRepository cargoRepository,
    IAgvErrorRepository errorRepository,
    IAgvPositionRepository agvPositionRepository,
    IBus bus
) : BackgroundService
{
    /// <inheritdoc cref="BackgroundService"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.WhenAll(InitializeTriggers(),
            cargoRepository.LoadInMemoryCache(),
            errorRepository.LoadInMemoryCache(),
            agvPositionRepository.RestorePersistedPositionsAsync(stoppingToken)
        );
    }

    private async Task InitializeTriggers()
    {
        await triggerRepository.LoadTriggerToLocationDictionary();
        ICollection<Trigger> triggers = await triggerRepository.GetTriggers();

        IEnumerable<object> triggerAssignments = triggers
            .SelectMany(trigger => trigger.Locations.Select(location => new { trigger, location }))
            .Select(tl => CreateTriggerAssignment(tl.trigger, tl.location))
            .Where(assignment => assignment != null!);

        await PublishTriggerAssignmentsBatch(triggerAssignments);
    }

    private object CreateTriggerAssignment(Trigger trigger, Location location)
    {
        if (location.NodeId != null)
        {
            return new TriggerAssignedToNode(trigger.Id, trigger.DisplayName, location.NodeId.Value);
        }

        if (location.Polygon.Count > 0)
        {
            return new TriggerAssignedToArea(trigger.Id, trigger.DisplayName, location.Id);
        }
        return null!;
    }

    private async Task PublishTriggerAssignmentsBatch(IEnumerable<object> assignments)
    {
        IEnumerable<Task> publishTasks = assignments.Select(assignment => bus.Publish(assignment));
        await Task.WhenAll(publishTasks);
    }
}