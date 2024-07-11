using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Trigger;

/// <summary>
/// Represents the event raised when a trigger location has been added or addition failed.
/// </summary>
/// <param name="Location">The new location.</param>
/// <param name="Exception">Exception thrown, if adding the location failed</param>
public record TriggerLocationAdded(Location? Location, string? Exception);