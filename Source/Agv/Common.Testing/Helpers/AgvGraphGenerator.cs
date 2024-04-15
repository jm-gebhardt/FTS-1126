using System.Numerics;
using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Extensions;

namespace Gebhardt.StoreWare.Agv.Common.Testing.Helpers;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
public static class AgvGraphGenerator
{
    ///////// Default graph for unit tests /////////
    // 0 ---- 1 ---- 2 ---- 3 ---- 4 ---- 5 ---- //
    // |                                         //
    // 1                   Src2          Src3    //
    // |                    |                    //
    // 2            Src1    |                    //
    // |             |      |                    //
    // 3     Prk1---Way1---Way2----------Prk2    //
    // |             |      |                    //
    // 4     Crg1---Way3---Way4----------Crg2    //
    // |             |      |                    //
    // 5            Dst1    |                    //
    // |                    |                    //
    // 6                   Dst2          Dst3    //
    // |                                         //
    ///////////////////////////////////////////////

    public const string Src1 = "SRC1"; // Source close
    private const string Src2 = "SRC2"; // Source far
    private const string Src3 = "SRC3"; // Source unreachable
    public const string Dst1 = "DST1"; // Destination close
    private const string Dst2 = "DST2"; // Destination far
    private const string Dst3 = "DST3"; // Destination unreachable
    public const string Prk1 = "PRK1"; // Parking spot close
    private const string Prk2 = "PRK2"; // Parking spot far
    public const string Crg1 = "CRG1"; // Charging point close
    private const string Crg2 = "CRG2"; // Charging point far
    public const string Way1 = "WAY1"; // Waypoint 1
    private const string Way2 = "WAY2"; // Waypoint 2
    public const string Way3 = "WAY3"; // Waypoint 3
    private const string Way4 = "WAY4"; // Waypoint 4

    /// <summary>
    /// Returns default nodes
    /// </summary>
    public static GraphNode[] Nodes { get; } =
    {
        new(Src1) { Type = AgvGraph.NodeType.Pick, X = 2f, Y = 2f, Z = 0f }, // 0
        new(Src2) { Type = AgvGraph.NodeType.Pick, X = 3f, Y = 1f, Z = 0f }, // 1
        new(Src3) { Type = AgvGraph.NodeType.Pick, X = 5f, Y = 1f, Z = 0f }, // 2 
        new(Dst1) { Type = AgvGraph.NodeType.Drop, X = 2f, Y = 5f, Z = 0f }, // 3 
        new(Dst2) { Type = AgvGraph.NodeType.Drop, X = 3f, Y = 6f, Z = 0f }, // 4
        new(Dst3) { Type = AgvGraph.NodeType.Drop, X = 5f, Y = 6f, Z = 0f }, // 5 
        new(Prk1) { Type = AgvGraph.NodeType.Park, X = 1f, Y = 3f, Z = 0f }, // 6 
        new(Prk2) { Type = AgvGraph.NodeType.Park, X = 5f, Y = 3f, Z = 0f }, // 7 
        new(Crg1) { Type = AgvGraph.NodeType.Charge, X = 1f, Y = 4f, Z = 0f }, // 8 
        new(Crg2) { Type = AgvGraph.NodeType.Charge, X = 5f, Y = 4f, Z = 0f }, // 9 
        new(Way1) { Type = AgvGraph.NodeType.WayPoint, X = 2f, Y = 3f, Z = 0f }, // 10
        new(Way2) { Type = AgvGraph.NodeType.WayPoint, X = 3f, Y = 3f, Z = 0f }, // 11
        new(Way3) { Type = AgvGraph.NodeType.WayPoint, X = 2f, Y = 4f, Z = 0f }, // 12
        new(Way4) { Type = AgvGraph.NodeType.WayPoint, X = 3f, Y = 4f, Z = 0f } // 13
    };

    public static GraphNode SourceClose { get; } = Nodes[0];

    public static GraphNode SourceFar { get; } = Nodes[1];

    public static GraphNode SourceUnreachable { get; } = Nodes[2];

    public static GraphNode DestinationClose { get; } = Nodes[3];

    public static GraphNode DestinationFar { get; } = Nodes[4];

    public static GraphNode DestinationUnreachable { get; } = Nodes[5];

    public static GraphNode ParkClose { get; } = Nodes[6];

    public static GraphNode ParkFar { get; } = Nodes[7];

    public static GraphNode ChargingClose { get; } = Nodes[8];

    public static GraphNode ChargingFar { get; } = Nodes[9];

    public static GraphNode Waypoint1 { get; } = Nodes[10];

    public static GraphNode Waypoint2 { get; } = Nodes[11];

    public static GraphNode Waypoint3 { get; } = Nodes[12];

    public static GraphNode Waypoint4 { get; } = Nodes[13];

    /// <summary>
    /// Returns default edges
    /// </summary>
    public static GraphEdge[] Edges { get; } =
    {
        // Src1 <-> Way1
        GraphEdge.CreateDirect(Src1, Way1),
        GraphEdge.CreateDirect(Way1, Src1),

        // Src2 <-> Way2
        GraphEdge.CreateDirect(Src2, Way2, 3f),
        GraphEdge.CreateDirect(Way2, Src2, 3f),

        // Dst1 <-> Way3
        GraphEdge.CreateDirect(Dst1, Way3),
        GraphEdge.CreateDirect(Way3, Dst1),

        // Dst2 <-> Way4
        GraphEdge.CreateDirect(Dst2, Way4, 3f),
        GraphEdge.CreateDirect(Way4, Dst2, 3f),

        // Prk1 <-> Way1
        GraphEdge.CreateDirect(Prk1, Way1),
        GraphEdge.CreateDirect(Way1, Prk1),

        // Prk2 <-> Way2
        GraphEdge.CreateDirect(Prk2, Way2, 3f),
        GraphEdge.CreateDirect(Way2, Prk2, 3f),

        // Crg1 <-> Way3
        GraphEdge.CreateDirect(Crg1, Way3),
        GraphEdge.CreateDirect(Way3, Crg1),

        // Crg2 <-> Way4
        GraphEdge.CreateDirect(Crg2, Way4, 3f),
        GraphEdge.CreateDirect(Way4, Crg2, 3f),

        // Way1 <-> Way2
        GraphEdge.CreateDirect(Way1, Way2),
        GraphEdge.CreateDirect(Way2, Way1),

        // Way2 <-> Way4
        GraphEdge.CreateDirect(Way2, Way4),
        GraphEdge.CreateDirect(Way4, Way2),

        // Way4 <-> Way3
        GraphEdge.CreateDirect(Way4, Way3),
        GraphEdge.CreateDirect(Way3, Way4),

        // Way3 <-> Way1
        GraphEdge.CreateDirect(Way3, Way1),
        GraphEdge.CreateDirect(Way1, Way3)
    };

    /// <summary>
    /// Generates a default agv graph
    /// </summary>
    public static AgvGraph CreateDefaultGraph(Guid? correlationId = null, ICollection<GraphNode>? nodes = null)
    {
        return new AgvGraph(correlationId ?? Guid.NewGuid(), nodes ?? Nodes, Edges);
    }

    public static AgvGraph CreateDefaultGraph(GraphNode[] nodes)
    {
        return new AgvGraph(Guid.NewGuid(), nodes, Edges);
    }

    public static AgvGraph Generate(IEnumerable<GraphNode> nodes, IEnumerable<GraphEdge> edges)
    {
        return new AgvGraph(Guid.NewGuid(), nodes.ToList(), edges.ToList());
    }

    public static GraphEdge CreateSlantedEdge(string fromNodeId, string toNodeId, float exitAngle, double exitDistance, GraphNode[] nodes)
    {
        var sourceNode = nodes.Single(n => n.Id == fromNodeId);
        var targetNode = nodes.Single(n => n.Id == toNodeId);
        double length = new Slanted(sourceNode.AsVector3(), targetNode.AsVector3(), exitAngle, exitDistance).GetLength();
        return GraphEdge.CreateSlanted(fromNodeId, toNodeId, (float)length, exitAngle, exitDistance);
    }

    public static GraphEdge CreatePolypathEdge(string fromNodeId, string toNodeId, List<EdgeCoefficient> pathPoints, GraphNode[] nodes)
    {
        var sourceNode = nodes.Single(n => n.Id == fromNodeId).AsVector3();
        var targetNode = nodes.Single(n => n.Id == toNodeId).AsVector3();
        List<Vector3> points = [sourceNode];
        points.AddRange(pathPoints.Select(c => new Vector3((float)c.X!, (float)c.Y!, 0f)));
        points.Add(targetNode);
        double length = new Polypath(points).GetLength();
        return GraphEdge.CreatePolypath(fromNodeId, toNodeId, (float)length, pathPoints);
    }
}

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global