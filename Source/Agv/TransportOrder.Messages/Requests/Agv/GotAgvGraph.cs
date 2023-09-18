using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Defines a result for the GetAgvGraph request
/// </summary>
public record GotAgvGraph(Graph AgvGraph);

/// <summary>
/// Message content for a graph 
/// </summary>
/// <param name="Nodes">The list of nodes in the graph.</param>
/// <param name="Edges">The list of edges in the graph.</param>
/// <param name="CorrelationId">CorrelationId</param>
[UsedImplicitly]
public record Graph(List<GraphNode> Nodes, List<GraphEdge> Edges, Guid CorrelationId) : CorrelatedBy<Guid>;

/// <summary>
/// Message content for a node in the graph.
/// </summary>
[UsedImplicitly]
public record GraphNode
{
    /// <summary>
    /// The Id of the node, based on the AGV vendor
    /// </summary><remarks>
    /// Usually this is the (numeric) ID passed by the AGV vendor.
    /// For internal reasons it can be necessary to build/combine an internal identification.
    /// </remarks>
    public string Id { get; init; } = null!;

    /// <summary>
    /// The type of the node
    /// </summary>
    public AgvGraph.NodeType Type { get; [UsedImplicitly] init; }

    /// <summary>
    /// The X coordinate of the node
    /// </summary>
    public float X { get; init; }

    /// <summary>
    /// The Y coordinate of the node
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    /// The Z coordinate of the node
    /// </summary>
    public float Z { get; init; }

    /// <summary>
    /// The Theta coordinate of the node
    /// </summary>
    public float Theta { get; init; }
}

/// <summary>
/// Message content for a graph edge.
/// </summary>
/// <param name="From">The from node.</param>
/// <param name="To">The to node.</param>
[UsedImplicitly]
public record GraphEdge(GraphNode From, GraphNode To);