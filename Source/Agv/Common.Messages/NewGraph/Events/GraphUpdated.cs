using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.NewGraph.Events;

/// <summary>
/// Event that is published when the graph has changed.
/// </summary>
/// <param name="CorrelationId">The correlation Id.</param>
/// <param name="Graph">The updated graph.</param>
public record GraphUpdated(Guid CorrelationId, Graph Graph) : CorrelatedBy<Guid>;