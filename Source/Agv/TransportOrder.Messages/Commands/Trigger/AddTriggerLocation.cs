using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Trigger;

/// <summary>
/// Provides a command to add a trigger location.
/// </summary>
/// <param name="TriggerId">The trigger id.</param>
/// <param name="EdgeId">The edge id, if set.</param>
/// <param name="NodeId">The node id, if set.</param>
/// <param name="Polygon">The polygon, if set.</param>
/// <param name="MapId">The map the location is on.</param>
public record AddTriggerLocation(Guid TriggerId, Guid? EdgeId, string? NodeId, ICollection<PolygonPoint> Polygon, Guid MapId) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public Guid CorrelationId => TriggerId;
}