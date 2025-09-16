using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;

/// <summary>
/// Represents the response to <see cref="AssignTriggerToLocation"/>.
/// </summary>
/// <param name="Location">The trigger location.</param>
/// <param name="Node">The node.</param>
public record TriggerLocationAssigned(Location Location, GraphNode? Node);