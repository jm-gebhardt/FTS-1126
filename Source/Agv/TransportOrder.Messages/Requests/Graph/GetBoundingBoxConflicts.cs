using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Graph;

/// <summary>
/// Represents a command to retrieve bounding box conflicts for all graph resources.
/// </summary>
[PublicAPI]
public record GetBoundingBoxConflicts(Guid CorrelationId) : CorrelatedBy<Guid>;