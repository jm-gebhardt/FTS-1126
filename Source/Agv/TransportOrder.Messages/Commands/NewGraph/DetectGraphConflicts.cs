using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.NewGraph;

/// <summary>
/// Represents a command to detect the conflicts between the routing resources of the graph and store them.
/// </summary>
[PublicAPI]
public record DetectGraphConflicts(Guid CorrelationId) : CorrelatedBy<Guid>;