using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.DeviceCommunication.Common.Helpers;
using Gebhardt.Acc.DeviceCommunication.Common.Services;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;

/// <summary>
/// Provides a fixed graph including all supported
/// curve types.
/// </summary>
public class FixedGraphGenerator : GraphGeneratorBase
{
    // Node names
    private const string Park1 = "Park-1";
    private const string Park2 = "Park-2";
    private const string Park3 = "Park-3";
    private const string Park4 = "Park-4";
    private const string Park5 = "Park-5";
    private const string Park6 = "Park-6";
    private const string Charge0 = "Charger-0";
    private const string Charge1 = "Charger-1";
    private const string Charge2 = "Charger-2";
    private const string Charge3 = "Charger-3";
    private const string Way0009 = "WP-0009";
    private const string Way0010 = "WP-0010";
    private const string Way0011 = "WP-0011";
    private const string Way0012 = "WP-0012";
    private const string Way0013 = "WP-0013";
    private const string Way0014 = "WP-0014";
    private const string Way0015 = "WP-0015";
    private const string Way0016 = "WP-0016";
    private const string Way0017 = "WP-0017";
    private const string Way0018 = "WP-0018";
    private const string Way0022 = "WP-0022";
    private const string Way0023 = "WP-0023";
    private const string Way0024 = "WP-0024";
    private const string Way0028 = "WP-0028";
    private const string Way0029 = "WP-0029";
    private const string Way0030 = "WP-0030";
    private const string Way0032 = "WP-0032";
    private const string Way0123 = "WP-0123";

    private const string DPaint = "D-Paint";
    private const string DPress = "D-Press";
    private const string DSaw = "D-Saw";
    private const string PDrill = "P-Drill";
    private const string PGrinder = "P-Grinder";
    private const string PMilling = "P-Milling";
    private const string Pdqa = "PD-QA";

/*
    /// <summary>
    /// Maximum edge velocity. Currently not respected.
    /// </summary>
    private const float MaxEdgeVelocity = 2.5f;
*/
    /// Station angles are defined as:
    /// 0
    /// 3pi/2 -|- pi/2
    /// pi
    private const float North = 0f;

    private const float East = (float) Math.PI / 2f;
    private const float West = 3f * (float) Math.PI / 2f;

    private const string MainMapName = "Charge Park";
    private const string SubMapName = "Stations";

    private readonly Guid _mainMapId = MainMapName.ToGuid();
    private readonly Guid _subMapId = SubMapName.ToGuid();

    /// <inheritdoc />
    protected override Guid GraphId { get; } = "FixedGraph".ToGuid();

    /// <inheritdoc cref="FixedGraphGenerator" />
    public FixedGraphGenerator(IMapGenerator mapGenerator) : base(new[] { Pdqa, PGrinder, PDrill, PMilling }, new[] { DPaint, DPress, DSaw, Pdqa }, mapGenerator)
    {
    }

    /// <inheritdoc />
    public override AgvGraph Generate(Guid correlationId)
    {
        var timeStamp = DateTime.UtcNow;
        var nodes = new GraphNode[]
        {
            new(Park1.ToGuid(), Park1) { Type = AgvGraph.NodeType.Park, X = 19.375f, Y = 43.125f, Z = 0f, Theta = West, TimeStamp = timeStamp },
            new(Park2.ToGuid(), Park2) { Type = AgvGraph.NodeType.Park, X = 33.125f, Y = 43.125f, Z = 0f, Theta = East, TimeStamp = timeStamp },
            new(Park3.ToGuid(), Park3) { Type = AgvGraph.NodeType.Park, X = 19.375f, Y = 34.375f, Z = 0f, Theta = West, TimeStamp = timeStamp },
            new(Park4.ToGuid(), Park4) { Type = AgvGraph.NodeType.Park, X = 31.875f, Y = 34.375f, Z = 0f, Theta = East, TimeStamp = timeStamp },
            new(Park5.ToGuid(), Park5) { Type = AgvGraph.NodeType.Park, X = 19.375f, Y = 29.375f, Z = 0f, Theta = West, TimeStamp = timeStamp },
            new(Park6.ToGuid(), Park6) { Type = AgvGraph.NodeType.Park, X = 31.875f, Y = 29.375f, Z = 0f, Theta = East, TimeStamp = timeStamp },

            new(Charge0.ToGuid(), Charge0) { Type = AgvGraph.NodeType.Charge, X = 46.875f, Y = 44.375f, Z = 0f, Theta = North, TimeStamp = timeStamp },
            new(Charge1.ToGuid(), Charge1) { Type = AgvGraph.NodeType.Charge, X = 46.875f, Y = 39.375f, Z = 0f, Theta = East, TimeStamp = timeStamp },
            new(Charge2.ToGuid(), Charge2) { Type = AgvGraph.NodeType.Charge, X = 46.875f, Y = 31.875f, Z = 0f, Theta = East, TimeStamp = timeStamp },
            new(Charge3.ToGuid(), Charge3) { Type = AgvGraph.NodeType.Charge, X = 46.875f, Y = 24.375f, Z = 0f, Theta = East, TimeStamp = timeStamp },

            new(DPaint.ToGuid(), DPaint) { Type = AgvGraph.NodeType.Drop, X = 93.295f, Y = 59.375f, Z = 0f, Theta = East, TimeStamp = timeStamp },
            new(DPress.ToGuid(), DPress) { Type = AgvGraph.NodeType.Drop, X = 93.295f, Y = 68.125f, Z = 0f, Theta = East, TimeStamp = timeStamp },
            new(DSaw.ToGuid(), DSaw) { Type = AgvGraph.NodeType.Drop, X = 93.17f, Y = 44.375f, Z = 0f, Theta = East, TimeStamp = timeStamp },

            new(PDrill.ToGuid(), PDrill) { Type = AgvGraph.NodeType.Pick, X = 14.375f, Y = 68.125f, Z = 0f, Theta = West, TimeStamp = timeStamp },
            new(PGrinder.ToGuid(), PGrinder) { Type = AgvGraph.NodeType.Pick, X = 14.375f, Y = 50.625f, Z = 0f, Theta = West, TimeStamp = timeStamp },
            new(PMilling.ToGuid(), PMilling) { Type = AgvGraph.NodeType.Pick, X = 14.375f, Y = 59.375f, Z = 0f, Theta = West, TimeStamp = timeStamp },

            new(Pdqa.ToGuid(), Pdqa) { Type = AgvGraph.NodeType.PickDrop, X = 92.965f, Y = 26.7425f, Z = 0f, Theta = (float) Mathematics.ConvertToRadians(73.4), TimeStamp = timeStamp },

            new(Way0009.ToGuid(), Way0009) { Type = AgvGraph.NodeType.WayPoint, X = 39.375f, Y = 68.125f, Z = 0f, Theta = 0, TimeStamp = timeStamp },
            new(Way0010.ToGuid(), Way0010) { Type = AgvGraph.NodeType.WayPoint, X = 64.375f, Y = 68.125f, Z = 0f, Theta = 0, TimeStamp = timeStamp },
            new(Way0011.ToGuid(), Way0011) { Type = AgvGraph.NodeType.WayPoint, X = 88.325f, Y = 68.125f, Z = 0f, Theta = 0, TimeStamp = timeStamp },
            new(Way0012.ToGuid(), Way0012) { Type = AgvGraph.NodeType.WayPoint, X = 21.875f, Y = 68.125f, Z = 0f, Theta = 0, TimeStamp = timeStamp },
            new(Way0013.ToGuid(), Way0013) { Type = AgvGraph.NodeType.WayPoint, X = 21.875f, Y = 59.375f, Z = 0f, Theta = 0, TimeStamp = timeStamp },
            new(Way0014.ToGuid(), Way0014) { Type = AgvGraph.NodeType.WayPoint, X = 33.125f, Y = 59.375f, Z = 0f, Theta = 0, TimeStamp = timeStamp },
            new(Way0015.ToGuid(), Way0015) { Type = AgvGraph.NodeType.WayPoint, X = 76.875f, Y = 59.375f, Z = 0f, Theta = 0, TimeStamp = timeStamp },
            new(Way0016.ToGuid(), Way0016) { Type = AgvGraph.NodeType.WayPoint, X = 88.325f, Y = 59.375f, Z = 0f, Theta = 0, TimeStamp = timeStamp },
            new(Way0017.ToGuid(), Way0017) { Type = AgvGraph.NodeType.WayPoint, X = 88.2f, Y = 44.375f, Z = 0f, Theta = 0, TimeStamp = timeStamp },
            new(Way0018.ToGuid(), Way0018) { Type = AgvGraph.NodeType.WayPoint, X = 46.875f, Y = 49.375f, Z = 0f, Theta = 0, TimeStamp = timeStamp },
            new(Way0022.ToGuid(), Way0022) { Type = AgvGraph.NodeType.WayPoint, X = 40.625f, Y = 39.375f, Z = 0f, Theta = 0, TimeStamp = timeStamp },
            new(Way0023.ToGuid(), Way0023) { Type = AgvGraph.NodeType.WayPoint, X = 40.625f, Y = 31.875f, Z = 0f, Theta = 0, TimeStamp = timeStamp },
            new(Way0024.ToGuid(), Way0024) { Type = AgvGraph.NodeType.WayPoint, X = 40.625f, Y = 24.375f, Z = 0f, Theta = 0, TimeStamp = timeStamp },
            new(Way0028.ToGuid(), Way0028) { Type = AgvGraph.NodeType.WayPoint, X = 25.625f, Y = 34.375f, Z = 0f, Theta = 0, TimeStamp = timeStamp },
            new(Way0029.ToGuid(), Way0029) { Type = AgvGraph.NodeType.WayPoint, X = 25.625f, Y = 43.125f, Z = 0f, Theta = 0, TimeStamp = timeStamp },
            new(Way0030.ToGuid(), Way0030) { Type = AgvGraph.NodeType.WayPoint, X = 25.625f, Y = 29.375f, Z = 0f, Theta = 0, TimeStamp = timeStamp },
            new(Way0032.ToGuid(), Way0032) { Type = AgvGraph.NodeType.WayPoint, X = 21.875f, Y = 50.625f, Z = 0f, Theta = 0, TimeStamp = timeStamp },
            new(Way0123.ToGuid(), Way0123) { Type = AgvGraph.NodeType.WayPoint, X = 88.325f, Y = 28.125f, Z = 0f, Theta = 0, TimeStamp = timeStamp }
        };

        // metric = length/maxEdgeVelocity
        // control points and edge coefficients are absolute values
        GraphEdge[] edges =
        {
            CreateDirect(Way0029, Park1, nodes),
            CreateDirect(Park1, Way0029, nodes),
            CreateDirect(Way0028, Park3, nodes),
            CreateDirect(Park3, Way0028, nodes),
            CreateDirect(Way0029, Park2, nodes),
            CreateDirect(Park2, Way0029, nodes),
            CreateDirect(Way0028, Park4, nodes),
            CreateDirect(Park4, Way0028, nodes),
            CreateDirect(Way0030, Park5, nodes),
            CreateDirect(Park5, Way0030, nodes),
            CreateDirect(Way0030, Park6, nodes),
            CreateDirect(Park6, Way0030, nodes),
            CreateDirect(Way0009, Way0010, nodes),
            CreateDirect(Way0010, Way0011, nodes),
            CreateDirect(Way0010, Way0015, nodes),
            CreateDirect(Way0011, Way0016, nodes),
            CreateDirect(Way0012, Way0009, nodes),
            CreateBezier2(Way0013, Way0009, 22.75d, 67.25d, nodes),
            CreateDirect(Way0013, Way0012, nodes),
            CreateDirect(Way0014, Way0013, nodes),
            CreatePolypath(Way0014, Way0029, new List<EdgeCoefficient> { new(32.25d, 50d) }, nodes),
            CreateBezier2(Way0014, Way0032, 31.375d, 51.125d, nodes),
            CreateDirect(Way0015, Way0014, nodes),

            CreatePolypath(Way0015, Way0017, new List<EdgeCoefficient>
            {
                new(76.875d, 51.875d), new(88.2d, 51.875d)
            }, nodes),
            CreateBezier2(Way0015, Way0018, 47.625d, 58.625d, nodes),
            CreateDirect(Way0016, Way0015, nodes),
            CreateBezier3(Way0017, Way0018, 80d, 52d, 54.375d, 45d, nodes),
            CreateDirect(Way0123, Way0017, nodes),
            CreateDirect(Way0017, Way0123, nodes),
            CreateSlanted(Way0018, Way0014, East, 3.375, nodes),
            CreateBezier2(Way0018, Way0022, 40.625d, 48.4d, nodes),
            CreateDirect(Way0022, Way0023, nodes),
            CreateDirect(Way0023, Way0024, nodes),
            CreatePolypath(Way0024, Way0030, new List<EdgeCoefficient> { new(31.25d, 25.125d) }, nodes),
            CreateDirect(Way0028, Way0029, nodes),
            CreateDirect(Way0029, Way0032, nodes),
            CreateDirect(Way0030, Way0028, nodes),
            CreateDirect(Way0032, Way0013, nodes),

            CreateDirect(Way0018, Charge0, nodes),
            CreateDirect(Charge0, Way0018, nodes),
            CreateDirect(Way0022, Charge1, nodes),
            CreateDirect(Charge1, Way0022, nodes),
            CreateDirect(Way0023, Charge2, nodes),
            CreateDirect(Charge2, Way0023, nodes),
            CreateDirect(Way0024, Charge3, nodes),
            CreateDirect(Charge3, Way0024, nodes),

            CreateDirect(Way0012, PDrill, nodes),
            CreateDirect(PDrill, Way0012, nodes),
            CreateDirect(Way0013, PMilling, nodes),
            CreateDirect(PMilling, Way0013, nodes),
            CreateDirect(Way0032, PGrinder, nodes),
            CreateDirect(PGrinder, Way0032, nodes),

            CreateDirect(Way0011, DPress, nodes),
            CreateDirect(DPress, Way0011, nodes),
            CreateDirect(Way0017, DSaw, nodes),
            CreateDirect(DSaw, Way0017, nodes),
            CreateDirect(Way0016, DPaint, nodes),
            CreateDirect(DPaint, Way0016, nodes),
            CreateDirect(Way0123, Pdqa, nodes),
            GraphEdge.CreateDirect(Pdqa.ToGuid(), Way0123.ToGuid(), 4.841580390930176f, GraphHelper.ToGuid(EdgeType.Direct, Pdqa, 30f, Way0123), timeStamp)
        };
        return new AgvGraph(correlationId, nodes, edges);
    }

    /// <inheritdoc cref="GraphEdge.CreateDirect"/>
    private static GraphEdge CreateDirect(string fromNodeId, string toNodeId, GraphNode[] nodes)
    {
        var sourceNode = nodes.Single(n => n.Name == fromNodeId);
        var targetNode = nodes.Single(n => n.Name == toNodeId);
        var timeStamp = new List<DateTime> { sourceNode.TimeStamp, targetNode.TimeStamp }.Max();
        float length = (float) Mathematics.EuclideanDistance(sourceNode, targetNode);
        return GraphEdge.CreateDirect(fromNodeId.ToGuid(), toNodeId.ToGuid(), length, GraphHelper.ToGuid(EdgeType.Direct, fromNodeId, length, toNodeId), timeStamp);
    }

    /// <inheritdoc cref="GraphEdge.CreateBezier2"/>
    private static GraphEdge CreateBezier2(string fromNodeId, string toNodeId, double x, double y, GraphNode[] nodes)
    {
        var timeStamp = new List<DateTime> { nodes.Single(n => n.Name == fromNodeId).TimeStamp, nodes.Single(n => n.Name == toNodeId).TimeStamp }.Max();
        float length = CalculateBezierLength(nodes, fromNodeId, toNodeId, [new Vector2((float) x, (float) y)]);
        return GraphEdge.CreateBezier2(fromNodeId.ToGuid(), toNodeId.ToGuid(), length, x, y, GraphHelper.ToGuid(EdgeType.Bezier2, fromNodeId, length, toNodeId), timeStamp);
    }

    /// <inheritdoc cref="GraphEdge.CreatePolypath"/>
    private static GraphEdge CreatePolypath(string fromNodeId, string toNodeId, List<EdgeCoefficient> pathPoints, GraphNode[] nodes)
    {
        var sourceNode = nodes.Single(n => n.Name == fromNodeId);
        var targetNode = nodes.Single(n => n.Name == toNodeId);
        var timeStamp = new List<DateTime> { sourceNode.TimeStamp, targetNode.TimeStamp }.Max();
        List<Vector3> points = [sourceNode.AsVector3()];
        points.AddRange(pathPoints.Select(c => new Vector3((float) c.X!, (float) c.Y!, 0f)));
        points.Add(targetNode.AsVector3());
        double length = new Polypath(points).GetLength();
        return GraphEdge.CreatePolypath(fromNodeId.ToGuid(), toNodeId.ToGuid(), (float) length, pathPoints, GraphHelper.ToGuid(EdgeType.Polypath, fromNodeId, (float) length, toNodeId), timeStamp);
    }

    /// <inheritdoc cref="GraphEdge.CreateBezier3"/>
    private static GraphEdge CreateBezier3(string fromNodeId, string toNodeId, double x1, double y1, double x2, double y2, GraphNode[] nodes)
    {
        var timeStamp = new List<DateTime> { nodes.Single(n => n.Name == fromNodeId).TimeStamp, nodes.Single(n => n.Name == toNodeId).TimeStamp }.Max();
        float length = CalculateBezierLength(nodes, fromNodeId, toNodeId, new List<Vector2> { new((float) x1, (float) y1), new((float) x2, (float) y2) });
        return GraphEdge.CreateBezier3(fromNodeId.ToGuid(), toNodeId.ToGuid(), length, x1, y1, x2, y2, GraphHelper.ToGuid(EdgeType.Bezier3, fromNodeId, length, toNodeId), timeStamp);
    }

    /// <inheritdoc cref="GraphEdge.CreateSlanted"/>
    private static GraphEdge CreateSlanted(string fromNodeId, string toNodeId, float exitAngle, double exitDistance, GraphNode[] nodes)
    {
        var sourceNode = nodes.Single(n => n.Name == fromNodeId);
        var targetNode = nodes.Single(n => n.Name == toNodeId);
        var timeStamp = new List<DateTime> { sourceNode.TimeStamp, targetNode.TimeStamp }.Max();
        double length = new Slanted(sourceNode.AsVector3(), targetNode.AsVector3(), exitAngle, exitDistance).GetLength();
        return GraphEdge.CreateSlanted(fromNodeId.ToGuid(), toNodeId.ToGuid(), (float) length, exitAngle, exitDistance, GraphHelper.ToGuid(EdgeType.Slanted, fromNodeId, (float) length, toNodeId), timeStamp);
    }

    /// <inheritdoc />
    protected override IEnumerable<Map> GenerateMapsBasedOnGlobalGraph(Graph globalGraph)
    {
        foreach (var map in base.GenerateMapsBasedOnGlobalGraph(globalGraph))
        {
            yield return map;
        }

        List<GraphNode> nodes = globalGraph.Nodes.Where(n => n.X <= 48).ToList();
        var graph = new Graph
        {
            Nodes = nodes,
            Edges = globalGraph.Edges.Where(e => nodes.Any(n => n.Id == e.FromNodeId) && nodes.Any(n => n.Id == e.ToNodeId)).ToList()
        };

        yield return MapGenerator.Generate(_mainMapId, MainMapName, graph);

        graph = new Graph()
        {
            Nodes = globalGraph.Nodes.Where(n => !graph.Nodes.Contains(n)).ToList(),
            Edges = globalGraph.Edges.Where(n => !graph.Edges.Contains(n)).ToList()
        };

        yield return MapGenerator.Generate(_subMapId, SubMapName, graph);
    }

    private static float CalculateBezierLength(GraphNode[] nodes, string fromNode, string toNode, List<Vector2> controlPoints)
    {
        var from = Array.Find(nodes, n => n.Name == fromNode);
        var to = Array.Find(nodes, n => n.Name == toNode);

        List<Vector2> controlPolygon = new() { from!.AsVector2() };
        controlPolygon.AddRange(controlPoints);
        controlPolygon.Add(to!.AsVector2());

        var bezier = new Bezier(controlPolygon);
        return (float) bezier.GetLength(100);
    }
}