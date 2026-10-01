using Gebhardt.Acc.Common.Domain.Models.NewGraph;

namespace Gebhardt.Acc.Common.Messages.NewGraph.Requests;

/// <summary>
/// Response with the (global) graph
/// </summary>
/// <param name="Graph">The graph.</param>
public record GotGraph(Graph Graph);