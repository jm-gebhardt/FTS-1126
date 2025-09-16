using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Triggers.Commands;

/// <summary>
/// Provides a command to add a trigger location.
/// </summary>
/// <param name="TriggerId">The trigger id.</param>
/// <param name="Node">The node, if set.</param>
/// <param name="EdgeId">The edge id, if set.</param>
/// <param name="Polygon">The polygon, if set.</param>
/// <param name="MapId">The map the location is on.</param>
/// <param name="ConfigurationParameters">Optional custom configuration parameters for the location.</param>
[PublicAPI]
public record AddTriggerLocation(Guid TriggerId, GraphNode? Node, Guid? EdgeId, ICollection<PolygonPoint> Polygon, Guid? MapId, ICollection<ConfigurationParameter>? ConfigurationParameters) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public Guid CorrelationId => TriggerId;
}