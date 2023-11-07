using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

/// <summary>
/// An edge in the graph connects two nodes.
/// </summary>
public class GraphEdge
{
    /// <summary>
    /// The Id of the edge.
    /// </summary>
    public Guid Id
    {
        get;
        [UsedImplicitly]
        init;
    } = Guid.NewGuid();

    /// <summary>
    /// The Id of the starting node of the edge.
    /// </summary>
    public string FromNodeId
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <summary>
    /// The Id of the ending node of the edge.
    /// </summary>
    public string ToNodeId
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <summary>
    /// Length of this edge in [m].
    /// </summary>
    public double Length
    {
        get;
        [UsedImplicitly]
        init;
    } = 1.0;

    /// <summary>
    /// Distance/costs to travel this edge (unitless quantifier).
    /// </summary>
    public float Metric { get; }

    /// <summary>
    /// The <see cref="EdgeType"/> of this edge.
    /// </summary>
    public EdgeType Variant { get; [UsedImplicitly]set; }

    /// <summary>
    /// The parameters for this edge type.
    /// </summary>
    public ICollection<EdgeCoefficient> VariantParameters { get; [UsedImplicitly] set; }

    /// <summary>
    /// Simple string representation.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"{FromNodeId} -- {Metric} --> {ToNodeId}";
    }

    /// <summary>
    /// Graph Edge Constructor.
    /// </summary>
    /// <param name="fromNodeId"></param>
    /// <param name="toNodeId"></param>
    /// <param name="metric"></param>
    /// <param name="variant"></param>
    /// <param name="variantParameters"></param>
    private GraphEdge(string fromNodeId, string toNodeId, float metric, EdgeType variant = EdgeType.Direct, ICollection<EdgeCoefficient>? variantParameters = null)
    {
        FromNodeId = fromNodeId;
        ToNodeId = toNodeId;
        Metric = metric;
        Variant = variant;
        VariantParameters = variantParameters ?? new List<EdgeCoefficient>();
    }

    /// <summary>
    /// Graph Edge Constructor.
    /// </summary>
    /// <param name="fromNodeId"></param>
    /// <param name="toNodeId"></param>
    /// <param name="metric"></param>
    /// <param name="id"></param>
    /// <param name="variant"></param>
    /// <param name="variantParameters"></param>
    private GraphEdge(string fromNodeId, string toNodeId, float metric, Guid id, EdgeType variant = EdgeType.Direct, ICollection<EdgeCoefficient>? variantParameters = null)
    {
        FromNodeId = fromNodeId;
        ToNodeId = toNodeId;
        Metric = metric;
        Id = id;
        Variant = variant;
        VariantParameters = variantParameters ?? new List<EdgeCoefficient>();
    }

    /// <summary>
    /// Ctor
    /// </summary>
    [UsedImplicitly]
    public GraphEdge()
    {
        FromNodeId = null!;
        ToNodeId = null!;
        VariantParameters = null!;
        Variant = default!;
    }

    /// <summary>
    /// Creates a new <see cref="EdgeType.Direct"/> <see cref="GraphEdge"/> instance.
    /// </summary>
    /// <param name="fromNodeId">The start point of the edge.</param>
    /// <param name="toNodeId">The end point of the edge.</param>
    /// <param name="metric">Metric for path finding.</param>
    /// <param name="id">Optional id.</param>
    /// <returns></returns>
    public static GraphEdge CreateDirect(string fromNodeId, string toNodeId, float metric, Guid? id = null)
    {
        return new GraphEdge(fromNodeId, toNodeId, metric, id ?? Guid.NewGuid());
    }

    /// <summary>
    /// Creates a new <see cref="EdgeType.Bezier1"/> <see cref="GraphEdge"/> instance.
    /// </summary>
    /// <param name="fromNodeId">The start point of the edge.</param>
    /// <param name="toNodeId">The end point of the edge.</param>
    /// <param name="metric">Metric for path finding.</param>
    /// <param name="x">The x coordinate of the bezier curve control polygon point.</param>
    /// <param name="y">The y coordinate of the bezier curve control polygon point.</param>
    /// <param name="id">Optional id.</param>
    [UsedImplicitly]
    public static GraphEdge CreateBezier1(string fromNodeId, string toNodeId, float metric, double x, double y, Guid? id = null)
    {
        return new GraphEdge(fromNodeId, toNodeId, metric, id ?? Guid.NewGuid(), EdgeType.Bezier1, new List<EdgeCoefficient> { new(x, y) });
    }

    /// <summary>
    /// Creates a new <see cref="EdgeType.Bezier2"/> <see cref="GraphEdge"/> instance.
    /// </summary>
    /// <param name="fromNodeId">The start point of the edge.</param>
    /// <param name="toNodeId">The end point of the edge.</param>
    /// <param name="metric">Metric for path finding.</param>
    /// <param name="x1">The x coordinate of the first bezier curve control polygon point.</param>
    /// <param name="y1">The y coordinate of the first bezier curve control polygon point.</param>
    /// <param name="x2">The x coordinate of the second bezier curve control polygon point.</param>
    /// <param name="y2">The y coordinate of the second bezier curve control polygon point.</param>
    /// <param name="id">Optional id.</param>
    [UsedImplicitly]
    public static GraphEdge CreateBezier2(string fromNodeId, string toNodeId, float metric, double x1, double y1, double x2, double y2, Guid? id = null)
    {
        return new GraphEdge(fromNodeId, toNodeId, metric, id ?? Guid.NewGuid(), EdgeType.Bezier2, new List<EdgeCoefficient> { new(x1, y1), new(x2, y2) });
    }

    /// <summary>
    /// Creates a new <see cref="EdgeType.Slanted"/> <see cref="GraphEdge"/> instance.
    /// </summary>
    /// <param name="fromNodeId">The start point of the edge.</param>
    /// <param name="toNodeId">The end point of the edge.</param>
    /// <param name="metric">Metric for path finding.</param>
    /// <param name="exitAngle">The angle that the node is exited on.</param>
    /// <param name="exitDistance">The distance to travel before entering the slant.</param>
    /// <param name="id">Optional id.</param>
    public static GraphEdge CreateSlanted(string fromNodeId, string toNodeId, float metric, double exitAngle, double exitDistance, Guid? id = null)
    {
        return new GraphEdge(fromNodeId, toNodeId, metric, id ?? Guid.NewGuid(), EdgeType.Slanted, new List<EdgeCoefficient> { new(exitAngle), new(exitDistance) });
    }

    /// <summary>
    /// Creates a new <see cref="EdgeType.Polypath"/> <see cref="GraphEdge"/> instance.
    /// </summary>
    /// <param name="fromNodeId">The start point of the edge.</param>
    /// <param name="toNodeId">The end point of the edge.</param>
    /// <param name="metric">Metric for path finding.</param>
    /// <param name="pathPoints">The points of the polypath (max 10).</param>
    /// <param name="id">Optional id.</param>
    /// <exception cref="ArgumentException">If the path is longer than ten points or one of the coefficients is not a point.</exception>
    [UsedImplicitly]
    public static GraphEdge CreatePolypath(string fromNodeId, string toNodeId, float metric, ICollection<EdgeCoefficient> pathPoints, Guid? id = null)
    {
        if (pathPoints.Any(ec => !ec.IsPoint))
        {
            throw new ArgumentException("Polypath definition contains non-point coefficient");
        }
        if (pathPoints.Count > 10)
        {
            throw new ArgumentException($"Polypath definition has too many coefficients: {pathPoints.Count} > 10");
        }
        return new GraphEdge(fromNodeId, toNodeId, metric, id ?? Guid.NewGuid(), EdgeType.Polypath, pathPoints);
    }
}