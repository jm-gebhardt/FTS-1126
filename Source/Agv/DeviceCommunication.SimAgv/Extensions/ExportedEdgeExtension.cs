using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.DeviceCommunication.Common.Helpers;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ExportedEdge"/>
/// </summary>
public static class ExportedEdgeExtension
{
    /// <summary>
    /// Gets the length of the exported edge
    /// </summary>
    /// <param name="edge">The edge length.</param>
    /// <param name="start">The start node of the edge.</param>
    /// <param name="end">The end node of the edge.</param>
    /// <returns>The length of the edge [m].</returns>
    private static float GetLength(this ExportedEdge edge, GraphNode start, GraphNode end)
    {
        if (edge.EdgeType == EdgeType.Direct)
        {
            return (float)Mathematics.EuclideanDistance(start.AsVector3(), end.AsVector3());
        }
        List<Vector2> points = [start.AsVector2(), .. edge.ControlPoints.Select(p => p.InvertY().AsVector2()).ToList(), end.AsVector2()];
        var bezier = new Bezier(points);
        return (float)bezier.GetLength(100);
    }

    /// <summary>
    /// Converts the exported edge to graph edges (including the reverse, if the edge is bidirectional).
    /// Only edges of type Direct and Bezier5 are supported.
    /// </summary>
    /// <param name="edge">The exported edge.</param>
    /// <param name="start">The start node of the edge.</param>
    /// <param name="end">The end node of the edge-</param>
    /// <returns>The edge as an GraphEdge and its reverse edge, if applicable.</returns>
    public static ICollection<GraphEdge> ToGraphEdges(this ExportedEdge edge, GraphNode start, GraphNode end)
    {
        float length = edge.GetLength(start, end);
        return edge.EdgeType switch
        {
            EdgeType.Direct => edge.ToDirectEdges(length),
            EdgeType.Bezier5 => edge.ToBezier5Edges(length),
            _ => []
        };
    }

    private static ICollection<GraphEdge> ToDirectEdges(this ExportedEdge edge, float length)
    {
        var graphEdge = GraphEdge.CreateDirect(edge.StartId, edge.EndId, length, GraphHelper.ToGuid(EdgeType.Direct, edge.StartId, length, edge.EndId));
        if (!edge.Bidirectional)
        {
            return [graphEdge];
        }
        var reverseGraphEdge = GraphEdge.CreateDirect(edge.EndId, edge.StartId, length, GraphHelper.ToGuid(EdgeType.Direct, edge.EndId, length, edge.StartId));
        return [graphEdge, reverseGraphEdge];
    }

    private static ICollection<GraphEdge> ToBezier5Edges(this ExportedEdge edge, float length)
    {
        EdgeCoefficient[] edgeCoefficients = edge.ControlPoints.Select(c => c.ToEdgeCoefficient()).ToArray();
        var graphEdge = GraphEdge.CreateBezier5(edge.StartId, edge.EndId, length,
            edgeCoefficients,
            GraphHelper.ToGuid(EdgeType.Bezier5, edge.StartId, length, edge.EndId));
        if (!edge.Bidirectional)
        {
            return [graphEdge];
        }
        var reverseGraphEdge = GraphEdge.CreateBezier5(edge.EndId, edge.StartId, length,
            edgeCoefficients.Reverse().ToArray(),
            GraphHelper.ToGuid(EdgeType.Bezier5, edge.EndId, length, edge.StartId));
        return [graphEdge, reverseGraphEdge];
    }
}