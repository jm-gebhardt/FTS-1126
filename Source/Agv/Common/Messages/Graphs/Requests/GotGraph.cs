using Gebhardt.StoreWare.Agv.Common.Models.Graphs;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Graphs.Requests;

/// <summary>
/// Response with the (global) graph
/// </summary>
/// <param name="Graph">The graph.</param>
public record GotGraph(Graph Graph);