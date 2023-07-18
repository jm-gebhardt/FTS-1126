using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

/// <summary>
/// An edge in the graph connects two nodes
/// </summary>
public class GraphEdge
{
    /// <summary>
    /// The Id of the edge
    /// </summary>
    public Guid Id
    {
        get;
        [UsedImplicitly]
        init;
    } = Guid.NewGuid();

    /// <summary>
    /// The Id of the starting node of the edge
    /// </summary>
    public string FromNodeId
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <summary>
    /// The Id of the ending node of the edge
    /// </summary>
    public string ToNodeId
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <summary>
    /// Distance/costs to travel this edge (unitless quantifier).
    /// </summary>
    public float Metric { get; init; }

    /// <summary>
    /// Simple string representation.
    /// </summary>
    /// <returns></returns>
    public override string ToString() => $"{FromNodeId} -- {Metric} --> {ToNodeId}";

    /// <summary>
    /// Graph Edge Constructor
    /// </summary>
    /// <param name="fromNodeId"></param>
    /// <param name="toNodeId"></param>
    /// <param name="metric"></param>
    public GraphEdge(string fromNodeId, string toNodeId, float metric)
    {
        FromNodeId = fromNodeId;
        ToNodeId = toNodeId;
        Metric = metric;
    }

    /// <summary>
    /// Graph Edge Constructor
    /// </summary>
    /// <param name="fromNodeId"></param>
    /// <param name="toNodeId"></param>
    /// <param name="metric"></param>
    /// <param name="id"></param>
    public GraphEdge(string fromNodeId, string toNodeId, float metric, Guid id)
    {
        FromNodeId = fromNodeId;
        ToNodeId = toNodeId;
        Metric = metric;
        Id = id;
    }

    /// <summary>
    /// Ctor
    /// </summary>
    [UsedImplicitly]
    public GraphEdge()
    {
        FromNodeId = null!;
        ToNodeId = null!;
    }
}