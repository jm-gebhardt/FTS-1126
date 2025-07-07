using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.DeviceCommunication.Common.Helpers;
using Gebhardt.Acc.DeviceCommunication.Common.Services;
using Gebhardt.Acc.DeviceCommunication.SimAgv.CmdOptions;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;

/// <summary>
/// Generate a grid graph <see cref="GridGraphGenerator"/> that uses Bezier curves to connect nodes.
/// </summary>
public class BezierGraphGenerator: GridGraphGenerator
{
    /// <inheritdoc />
    protected override Guid GraphId => $"SIM_Bezier_{GridWidth}x{GridRows}".ToGuid();

    private static readonly Random _random = new();

    /// <inheritdoc />
    public BezierGraphGenerator(Options options, IMapGenerator mapGenerator) : base(options, mapGenerator){}

    /// <inheritdoc />
    public BezierGraphGenerator(
        string[] sources,
        string[] destinations,
        IMapGenerator mapGenerator,
        int gridRows = 3,
        AgvGraph.NodeType[]? serviceNodes = null) : base(sources, destinations, mapGenerator, gridRows, serviceNodes){}

    /// <summary>
    /// Generate AgvGraph
    /// </summary>
    /// <returns></returns>
    public override AgvGraph Generate(Guid correlationId)
    {
        // Prevent re-creation of graph for new agv.
        if (OtherNodes.Count == 0)
        {
            Clear();
            AddGrid();
            AddSourceNodes();
            AddDestinationNodes();
            AddServiceNodes();
        }

        return new AgvGraph(
            correlationId,
            OtherNodes.Concat(GridNodes).ToArray(),
            Edges.ToArray()
        );
    }

    /// <inheritdoc />
    protected override void AddEdge(GraphNode fromNode, GraphNode toNode, bool bidirectional = true)
    {
        Vector2[] controlPoints = GetControlPoints(fromNode, toNode);
        float length = GetLength(controlPoints.ToList());
        EdgeCoefficient[] edgeCoefficients = controlPoints.Skip(1).Take(4).Select(c => new EdgeCoefficient(c.X, c.Y)).ToArray();
        var timeStamp = new List<DateTime> { fromNode.TimeStamp, toNode.TimeStamp }.Max();
        
        Edges.Add(
            GraphEdge.CreateBezier5(fromNode.Id, toNode.Id, length, edgeCoefficients,
                GraphHelper.ToGuid(EdgeType.Bezier5, fromNode.Id, length, toNode.Id),
                timeStamp)
            );
        
        if (bidirectional)
        {
            Edges.Add(
                GraphEdge.CreateBezier5(toNode.Id, fromNode.Id, length, 
                    edgeCoefficients.Reverse().ToArray(), 
                    GraphHelper.ToGuid(EdgeType.Bezier5, toNode.Id, length, fromNode.Id),
                    timeStamp)
                );
        }
    }

    private static Vector2[] GetControlPoints(GraphNode startNode, GraphNode endNode)
    {
        if (startNode.IsStation() || endNode.IsStation())
        {
            return GenerateLinearControlPoints(startNode.AsVector2(), endNode.AsVector2());
        }
        return GenerateRandomControlPoints(startNode.AsVector2(), endNode.AsVector2());
    }

    /// <summary>
    /// Generate six evenly distributed control points between start and end (including start and end).
    /// </summary>
    /// <param name="start">The start.</param>
    /// <param name="end">The end.</param>
    /// <returns>Array of control points including start and end.</returns>
    private static Vector2[] GenerateLinearControlPoints(Vector2 start, Vector2 end)
    {
        const float increment = 0.2f;
        return Enumerable.Range(0, 6)
            .Select(i => start + i * increment * (end - start))
            .ToArray();
    }

    /// <summary>
    /// Generate six randomly distributed control points between start and end (including start and end).
    /// 1) Get the direction from start to end, and the middle between the two points.
    /// 2) Generate a vector perpendicular to the direction.
    /// 3) Generate 4 control points along the direction with random deviations on the perpendicular 
    /// </summary>
    /// <param name="start">The start</param>
    /// <param name="end">The end</param>
    /// <returns>Array of control points including start and end.</returns>
    private static Vector2[] GenerateRandomControlPoints(Vector2 start, Vector2 end)
    {
        var direction = end - start;
        var mid = (start + end) / 2;
        var perpendicular = new Vector2(-direction.Y, direction.X);
        const float fraction20 = 0.2f;
        const float fraction30 = 0.3f;

        var p1 = start + fraction20 * direction + fraction20 * RandomPerpendicular(perpendicular);
        var p2 = mid - fraction20 * direction + fraction30 * RandomPerpendicular(perpendicular);
        var p3 = mid + fraction30 * RandomPerpendicular(perpendicular);
        var p4 = mid + fraction20 * direction + fraction30 * RandomPerpendicular(perpendicular);

        return [start, p1, p2, p3, p4, end];
    }

    private static Vector2 RandomPerpendicular(Vector2 perpendicular)
    {
        float scale = (float)(_random.NextDouble() * 2 - 1);
        return scale * perpendicular;
    }

    private static float GetLength(List<Vector2> controlPoints)
    {
        var bezier = new Bezier(controlPoints);
        return (float)bezier.GetLength(100);
    }
}