using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.Triggers;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;

/// <summary>
/// Provides a request to assign a trigger to a location.
/// </summary>
/// <param name="TriggerId">The trigger id.</param>
/// <param name="Node">The node if set.</param>
/// <param name="EdgeId">The edge id, if set.</param>
/// <param name="Polygon">The polygon, if set.</param>
/// <param name="MapId">The map the location is on.</param>
/// <param name="ConfigurationParameters">Optional custom configuration parameters for the location.</param>
public record AssignTriggerToLocation(Guid TriggerId, GraphNode? Node, Guid? EdgeId, ICollection<PolygonPoint> Polygon, Guid? MapId, ICollection<ConfigurationParameter>? ConfigurationParameters);