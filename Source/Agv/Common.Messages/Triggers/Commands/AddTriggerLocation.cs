using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Triggers.Commands;

/// <summary>
/// Provides a command to add a trigger location.
/// </summary>
/// <param name="TriggerId">The trigger id.</param>
/// <param name="NodeId">The node id, if set.</param>
/// <param name="EdgeId">The edge id, if set.</param>
/// <param name="Polygon">The polygon, if set.</param>
/// <param name="MapId">The map the location is on.</param>
[PublicAPI]
public record AddTriggerLocation(Guid TriggerId, string? NodeId, Guid? EdgeId, ICollection<PolygonPoint> Polygon, Guid? MapId) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public Guid CorrelationId => TriggerId;
}