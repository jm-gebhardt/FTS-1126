using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Graph;

/// <summary>
/// Represents a command to retrieve bounding box conflicts involving a single graph edge.
/// </summary>
[PublicAPI]
public record GetEdgeBoundingBoxConflicts(Guid CorrelationId, Guid EdgeId) : CorrelatedBy<Guid>;