using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Graph;

/// <summary>
/// Represents a command to retrieve bounding box conflicts involving a single graph node.
/// </summary>
[PublicAPI]
public record GetNodeBoundingBoxConflicts(Guid CorrelationId, Guid NodeId) : CorrelatedBy<Guid>;