using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;

/// <summary>
/// Represents the response to <see cref="AssignTriggerToLocation"/>.
/// </summary>
/// <param name="Location">The trigger location.</param>
public record TriggerLocationAssigned(Location Location);