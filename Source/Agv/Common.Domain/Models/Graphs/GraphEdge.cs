using Gebhardt.Acc.Common.Domain.Utilities;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models.Graphs;

/// <summary>
/// An edge in the graph connects two nodes.
/// </summary>
public class GraphEdge : IEquatable<GraphEdge>
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
    /// The <see cref="EdgeType"/> of this edge.
    /// </summary>
    public EdgeType Variant
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <summary>
    /// The parameters for this edge type.
    /// </summary>
    public ICollection<EdgeCoefficient> VariantParameters
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <summary>
    /// Simple string representation.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"{Variant} -- {FromNodeId} -- {Length} --> {ToNodeId}";
    }

    /// <summary>
    /// Graph Edge Constructor.
    /// </summary>
    /// <param name="fromNodeId">Id of the start node of the edge.</param>
    /// <param name="toNodeId">Id of the end node of the edge.</param>
    /// <param name="length">The Length of the edge in [m]</param>
    /// <param name="id">The id of the edge.</param>
    /// <param name="variant">The <see cref="EdgeType"/> of the edge.</param>
    /// <param name="variantParameters">The parameters belonging to the <see cref="EdgeType"/></param>
    public GraphEdge(string fromNodeId, string toNodeId, float length, Guid id, EdgeType variant = EdgeType.Direct, ICollection<EdgeCoefficient>? variantParameters = null)
    {
        FromNodeId = fromNodeId;
        ToNodeId = toNodeId;
        Length = length;
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

    /// <inheritdoc />
    public bool Equals(GraphEdge? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        if (!Id.Equals(other.Id))
        {
            return false;
        }
        if (FromNodeId != other.FromNodeId)
        {
            return false;
        }
        if (ToNodeId != other.ToNodeId)
        {
            return false;
        }
        if (!Length.Equals(other.Length))
        {
            return false;
        }
        if (Variant != other.Variant)
        {
            return false;
        }
        if (!VariantParameters.SequenceEqual(other.VariantParameters))
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj.GetType() == GetType() && Equals((GraphEdge)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, FromNodeId, ToNodeId, Length, (int)Variant, VariantParameters);
    }

    /// <summary>
    /// Creates a new <see cref="EdgeType.Direct"/> <see cref="GraphEdge"/> instance.
    /// </summary>
    /// <param name="fromNodeId">The start point of the edge.</param>
    /// <param name="toNodeId">The end point of the edge.</param>
    /// <param name="length">Length of the edge in [m].</param>
    /// <param name="id">Optional id.</param>
    /// <returns></returns>
    public static GraphEdge CreateDirect(string fromNodeId, string toNodeId, float length = 1f, Guid? id = null)
    {
        return new GraphEdge(fromNodeId, toNodeId, length, id ?? Guid.NewGuid());
    }

    /// <summary>
    /// Creates a new <see cref="EdgeType.Bezier2"/> <see cref="GraphEdge"/> instance.
    /// </summary>
    /// <param name="fromNodeId">The start point of the edge.</param>
    /// <param name="toNodeId">The end point of the edge.</param>
    /// <param name="length">Length of the edge in [m].</param>
    /// <param name="x">The x coordinate of the bezier curve control polygon point.</param>
    /// <param name="y">The y coordinate of the bezier curve control polygon point.</param>
    /// <param name="id">Optional id.</param>
    [UsedImplicitly]
    public static GraphEdge CreateBezier2(string fromNodeId, string toNodeId, float length, double x, double y, Guid? id = null)
    {
        return new GraphEdge(fromNodeId, toNodeId, length, id ?? Guid.NewGuid(), EdgeType.Bezier2, new List<EdgeCoefficient> { new(x, y) });
    }

    /// <summary>
    /// Creates a new <see cref="EdgeType.Bezier3"/> <see cref="GraphEdge"/> instance.
    /// </summary>
    /// <param name="fromNodeId">The start point of the edge.</param>
    /// <param name="toNodeId">The end point of the edge.</param>
    /// <param name="length">Length of the edge in [m].</param>
    /// <param name="x1">The x coordinate of the first bezier curve control polygon point.</param>
    /// <param name="y1">The y coordinate of the first bezier curve control polygon point.</param>
    /// <param name="x2">The x coordinate of the second bezier curve control polygon point.</param>
    /// <param name="y2">The y coordinate of the second bezier curve control polygon point.</param>
    /// <param name="id">Optional id.</param>
    [UsedImplicitly]
    public static GraphEdge CreateBezier3(string fromNodeId, string toNodeId, float length, double x1, double y1, double x2, double y2, Guid? id = null)
    {
        return new GraphEdge(fromNodeId, toNodeId, length, id ?? Guid.NewGuid(), EdgeType.Bezier3, new List<EdgeCoefficient> { new(x1, y1), new(x2, y2) });
    }

    /// <summary>
    /// Creates a new <see cref="EdgeType.Bezier5"/> <see cref="GraphEdge"/> instance.
    /// </summary>
    /// <param name="fromNodeId">The start point of the edge.</param>
    /// <param name="toNodeId">The end point of the edge.</param>
    /// <param name="length">Length of the edge in [m].</param>
    /// <param name="controlPoints">The control points except start and end of the edge.</param>
    /// <param name="id">Optional id.</param>
    [UsedImplicitly]
    public static GraphEdge CreateBezier5(string fromNodeId, string toNodeId, float length, EdgeCoefficient[] controlPoints, Guid? id = null)
    {
        if (controlPoints.Length != 4)
        {
            throw new Exception("Wrong number of control points, should be 4 excluding source and target for Bezier 5");
        }
        return new GraphEdge(fromNodeId, toNodeId, length, id ?? Guid.NewGuid(), EdgeType.Bezier5, controlPoints);
    }

    /// <summary>
    /// Creates a new <see cref="EdgeType.Slanted"/> <see cref="GraphEdge"/> instance.
    /// </summary>
    /// <param name="fromNodeId">The start point of the edge.</param>
    /// <param name="toNodeId">The end point of the edge.</param>
    /// <param name="length">Length of the edge in [m].</param>
    /// <param name="exitAngle">The angle that the node is exited on.</param>
    /// <param name="exitDistance">The distance to travel before entering the slant.</param>
    /// <param name="id">Optional id.</param>
    public static GraphEdge CreateSlanted(string fromNodeId, string toNodeId, float length, double exitAngle, double exitDistance, Guid? id = null)
    {
        return new GraphEdge(fromNodeId, toNodeId, length, id ?? Guid.NewGuid(), EdgeType.Slanted, new List<EdgeCoefficient> { new(exitAngle), new(exitDistance) });
    }

    /// <summary>
    /// Creates a new <see cref="EdgeType.Polypath"/> <see cref="GraphEdge"/> instance.
    /// </summary>
    /// <param name="fromNodeId">The start point of the edge.</param>
    /// <param name="toNodeId">The end point of the edge.</param>
    /// <param name="length">Length of the edge in [m].</param>
    /// <param name="pathPoints">The points of the polypath (max 10).</param>
    /// <param name="id">Optional id.</param>
    /// <exception cref="ArgumentException">If the path is longer than ten points or one of the coefficients is not a point.</exception>
    [UsedImplicitly]
    public static GraphEdge CreatePolypath(string fromNodeId, string toNodeId, float length, ICollection<EdgeCoefficient> pathPoints, Guid? id = null)
    {
        if (pathPoints.Any(ec => !ec.IsPoint))
        {
            throw new ArgumentException("Polypath definition contains non-point coefficient");
        }
        if (pathPoints.Count > 10)
        {
            throw new ArgumentException($"Polypath definition has too many coefficients: {pathPoints.Count} > 10");
        }
        return new GraphEdge(fromNodeId, toNodeId, length, id ?? Guid.NewGuid(), EdgeType.Polypath, pathPoints);
    }

    /// <summary>
    /// Get subsections of edge for <see cref="EdgeType.Polypath"/> and <see cref="EdgeType.Slanted"/>.
    /// </summary>
    /// <param name="sourceNode">The source <see cref="GraphNode"/>> of the edge.</param>
    /// <param name="targetNode">The target <see cref="GraphNode"/>> of the edge.</param>
    /// <returns></returns>
    public (List<GraphEdge> sections, List<GraphNode> virtualNodes) GetSections(GraphNode sourceNode, GraphNode targetNode)
    {
        (List<GraphEdge> sections, List<GraphNode> virtualNodes) empty = new([], []);
        return Variant switch
        {
            EdgeType.Polypath => GetPolyPathSections(sourceNode, targetNode),
            EdgeType.Slanted => GetSlantedSections(sourceNode, targetNode),
            _ => empty
        };
    }

    private (List<GraphEdge> sections, List<GraphNode> virtualNodes) GetPolyPathSections(GraphNode sourceNode, GraphNode targetNode)
    {
        var source = sourceNode;
        var sections = new List<GraphEdge>();
        var virtualNodes = new List<GraphNode>();
        List<EdgeCoefficient> controlPoints = VariantParameters.ToList();
        GraphEdge? section;
        foreach (var controlPoint in controlPoints.Select((edgeCoefficient, index) => new { edgeCoefficient, index }))
        {
            var target = new GraphNode(Guid.NewGuid().ToString()) { X = (float)controlPoint.edgeCoefficient.X!, Y = (float)controlPoint.edgeCoefficient.Y! };
            virtualNodes.Add(target);
            section = CreateDirect(source.Id, target.Id, (float)Mathematics.EuclideanDistance(source, target), Guid.NewGuid());
            sections.Add(section);
            source = target;
        }
        section = CreateDirect(source.Id, targetNode.Id, (float)Mathematics.EuclideanDistance(source, targetNode), Guid.NewGuid());
        sections.Add(section);
        return (sections, virtualNodes);
    }

    private (List<GraphEdge> sections, List<GraphNode> virtualNodes) GetSlantedSections(GraphNode sourceNode, GraphNode targetNode)
    {
        var source = sourceNode;
        var sections = new List<GraphEdge>();
        GraphEdge? section;
        double angle = (double)VariantParameters.First().Scalar!;
        double distance = (double)VariantParameters.Last().Scalar!;
        double outAngle = 2 * Math.PI + angle; // 
        List<GraphNode> virtualNodes =
        [
            new(Guid.NewGuid().ToString()) { X = sourceNode.X - (float)(distance * Math.Sin(angle)), Y = sourceNode.Y - (float)(distance * Math.Cos(angle)) },
            new(Guid.NewGuid().ToString()) { X = targetNode.X + (float)(distance * Math.Sin(outAngle)), Y = targetNode.Y + (float)(distance * Math.Cos(outAngle)) }
        ];
        foreach (var virtualNode in virtualNodes)
        {
            section = CreateDirect(source.Id, virtualNode.Id, (float)Mathematics.EuclideanDistance(source, virtualNode), Guid.NewGuid());
            sections.Add(section);
            source = virtualNode;
        }
        section = CreateDirect(source.Id, targetNode.Id, (float)Mathematics.EuclideanDistance(source, targetNode), Guid.NewGuid());
        sections.Add(section);
        return (sections, virtualNodes);
    }
}