using MassTransit;
using Graph = Gebhardt.Acc.Common.Domain.Models.NewGraph.Graph;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.NewGraph;

/// <summary>
/// Represents a command to merge a graph into the stored graph.
/// </summary>
/// <param name="CorrelationId">The identifier of this message.</param>
/// <param name="Graph">The graph to save.</param>
public record AddNewGraph(Guid CorrelationId, Graph Graph) : CorrelatedBy<Guid>;