using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using JetBrains.Annotations;
using System.Numerics;
using GraphNode = Gebhardt.Acc.Common.Domain.Models.NewGraph.Node;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Graph;

/// <summary>
/// Base class for edge polygon providers. Subclasses implement <see cref="Supports"/> and <see cref="GetPolygon"/>. This base class provides helper methods for working with polygons.
/// </summary>
[PublicAPI]
public abstract class EdgePolygonProviderBase : IEdgePolygonProvider
{
    /// <inheritdoc/>
    public abstract bool Supports(Trajectory? trajectory);

    /// <inheritdoc/>
    public abstract double GetDirectionAtEndpoint(RoutingEdge edge, GraphNode startNode, GraphNode endNode);

    /// <inheritdoc/>
    public abstract Polygon GetPolygon(RoutingEdge edge, GraphNode startNode, GraphNode endNode, IAgvType agvType, IReadOnlyList<IMarginStrategy> margins);

    /// <summary>
    /// Gets the polygon representing the AGV with applied margins for the edge at a specific sample position.
    /// </summary>
    /// <returns>The polygon representing the AGV with applied margins.</returns>
    protected Polygon GetEdgeSamplePolygon(RoutingEdge edge, IAgvType agvType, IReadOnlyList<IMarginStrategy> marginStrategies, double tangentialAgvOrientation)
    {
        double halfLength = agvType.Length / 2;
        double halfWidth = agvType.Width / 2;

        // Local corners CCW: front-left, back-left, back-right, front-right
        var agvPolygon = new Polygon([
            new PolygonPoint(halfLength, halfWidth),
            new PolygonPoint(-halfLength, halfWidth),
            new PolygonPoint(-halfLength, -halfWidth),
            new PolygonPoint(halfLength, -halfWidth)
        ]);

        var rotatedPolygon = agvPolygon.Rotate(tangentialAgvOrientation);

        return marginStrategies.Aggregate(rotatedPolygon, (c, ms) => ms.Apply(c));
    }

    /// <summary>
    /// Splits the vertices of a convex polygon into two chains representing the start and end caps of an edge polygon and identifies the inner and outer vertices for lateral offset.
    /// </summary>
    /// <param name="vertices"></param>
    /// <returns></returns>
    protected (Vector2 InnerVertex, Vector2 OuterVertex, List<Vector2> StartCap, List<Vector2> EndCap) SplitPolygon(List<Vector2> vertices)
    {
        // The lateral offset of a transformed vertex equals its local Y coordinate, so the inner (min Y) and outer
        // (max Y) silhouette vertices are independent of the step. Ties are broken towards the front (max local X)
        // so that the caps split the boundary at the leading corners.
        var innerVertex = vertices.OrderBy(vertex => vertex.Y).ThenByDescending(vertex => vertex.X).First();
        var outerVertex = vertices.OrderByDescending(vertex => vertex.Y).ThenByDescending(vertex => vertex.X).First();
        int innerIndex = vertices.IndexOf(innerVertex);
        int outerIndex = vertices.IndexOf(outerVertex);

        // The caps are the counter-clockwise boundary chains between the two lateral extremes. Because the polygon
        // is convex and ordered CCW, the chain from the inner to the outer vertex is the leading (front) face and
        // the chain from the outer back to the inner vertex is the trailing (rear) face. Taking the whole chain -
        // instead of only the extreme-X vertices - preserves slanted or pointed faces of an arbitrary convex shape.
        var endCap = BoundaryChain(vertices, innerIndex, outerIndex);
        var startCap = BoundaryChain(vertices, outerIndex, innerIndex);

        return (innerVertex, outerVertex, startCap, endCap);
    }

    /// <summary>
    /// Transforms a vertex given in local bounding box coordinates into world coordinates using the provided frame.
    /// </summary>
    protected static Vector2 Transform((Vector2 Point, Vector2 Tangent, Vector2 Normal) frame, Vector2 local)
    {
        return frame.Point + frame.Tangent * local.X + frame.Normal * local.Y;
    }

    /// <summary>
    /// Returns the counter-clockwise boundary chain of a CCW-ordered polygon from <paramref name="fromIndex"/> to
    /// <paramref name="toIndex"/> (both inclusive), wrapping around the end of the vertex list if necessary.
    /// </summary>
    private static List<Vector2> BoundaryChain(List<Vector2> polygon, int fromIndex, int toIndex)
    {
        var chain = new List<Vector2>();
        int index = fromIndex;
        while (true)
        {
            chain.Add(polygon[index]);
            if (index == toIndex)
            {
                break;
            }
            index = (index + 1) % polygon.Count;
        }
        return chain;
    }
}