using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.Common.Messages.Graphs.Requests;

/// <summary>
/// Response with the (global) graph
/// </summary>
/// <param name="Graph">The graph.</param>
public record GotGraph(Graph Graph);