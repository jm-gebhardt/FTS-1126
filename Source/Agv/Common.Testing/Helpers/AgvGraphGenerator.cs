using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;

namespace Gebhardt.Acc.Common.Testing.Helpers;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
public static class AgvGraphGenerator
{
    ///////// Default graph for unit tests /////////
    // 0 ---- 1 ---- 2 ---- 3 ---- 4 ---- 5 ---- //
    // |                                         //
    // 1                   Src2          Src3    //
    // |                    |    Dbl1            //
    // 2            Src1    |   /                //
    // |             |      |  /                 //
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
    private const string Dbl1 = "DBL1"; // Station with double type: charge & park

    public static GraphNode Src1Node { get; } = new(Src1.ToGuid(), Src1) { Type = AgvGraph.NodeType.Pick, X = 2f, Y = 2f, Z = 0f };

    public static GraphNode Src2Node { get; } = new(Src2.ToGuid(), Src2) { Type = AgvGraph.NodeType.Pick, X = 3f, Y = 1f, Z = 0f };

    public static GraphNode Src3Node { get; } = new(Src3.ToGuid(), Src3) { Type = AgvGraph.NodeType.Pick, X = 5f, Y = 1f, Z = 0f };

    public static GraphNode Dst1Node { get; } = new(Dst1.ToGuid(), Dst1) { Type = AgvGraph.NodeType.Drop, X = 2f, Y = 5f, Z = 0f };

    public static GraphNode Dst2Node { get; } = new(Dst2.ToGuid(), Dst2) { Type = AgvGraph.NodeType.Drop, X = 3f, Y = 6f, Z = 0f };

    public static GraphNode Dst3Node { get; } = new(Dst3.ToGuid(), Dst3) { Type = AgvGraph.NodeType.Drop, X = 5f, Y = 6f, Z = 0f };

    public static GraphNode Prk1Node { get; } = new(Prk1.ToGuid(), Prk1) { Type = AgvGraph.NodeType.Park, X = 1f, Y = 3f, Z = 0f };

    public static GraphNode Prk2Node { get; } = new(Prk2.ToGuid(), Prk2) { Type = AgvGraph.NodeType.Park, X = 5f, Y = 3f, Z = 0f };

    public static GraphNode Crg1Node { get; } = new(Crg1.ToGuid(), Crg1) { Type = AgvGraph.NodeType.Charge, X = 1f, Y = 4f, Z = 0f };

    public static GraphNode Crg2Node { get; } = new(Crg2.ToGuid(), Crg2) { Type = AgvGraph.NodeType.Charge, X = 5f, Y = 4f, Z = 0f };

    public static GraphNode Way1Node { get; } = new(Way1.ToGuid(), Way1) { Type = AgvGraph.NodeType.WayPoint, X = 2f, Y = 3f, Z = 0f };

    public static GraphNode Way2Node { get; } = new(Way2.ToGuid(), Way2) { Type = AgvGraph.NodeType.WayPoint, X = 3f, Y = 3f, Z = 0f };

    public static GraphNode Way3Node { get; } = new(Way3.ToGuid(), Way3) { Type = AgvGraph.NodeType.WayPoint, X = 2f, Y = 4f, Z = 0f };

    public static GraphNode Way4Node { get; } = new(Way4.ToGuid(), Way4) { Type = AgvGraph.NodeType.WayPoint, X = 3f, Y = 4f, Z = 0f };

    /// <summary>
    /// Returns default nodes
    /// </summary>
    public static GraphNode[] Nodes { get; } =
    {
        Src1Node, // 0
        Src2Node, // 1
        Src3Node, // 2
        Dst1Node, // 3
        Dst2Node, // 4
        Dst3Node, // 5
        Prk1Node, // 6
        Prk2Node, // 7
        Crg1Node, // 8
        Crg2Node, // 9
        Way1Node, // 10
        Way2Node, // 11
        Way3Node, // 12
        Way4Node // 13
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

    public static GraphNode Double1 { get; } = new(Dbl1.ToGuid(), Dbl1) { Type = new List<AgvGraph.NodeType> { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park }, X = 4f, Y = 1.5f, Z = 0f };

    public static GraphEdge[] EdgesToDbl1 { get; } =
    {
        // Way2 <-> Dbl1
        GraphEdge.CreateDirect(Dbl1.ToGuid(), Way2.ToGuid()),
        GraphEdge.CreateDirect(Way2.ToGuid(), Dbl1.ToGuid())
    };

    /// <summary>
    /// Returns default edges
    /// </summary>
    public static GraphEdge[] Edges { get; } =
    {
        // Src1 <-> Way1
        GraphEdge.CreateDirect(Src1Node.Id, Way1Node.Id),
        GraphEdge.CreateDirect(Way1Node.Id, Src1Node.Id),

        // Src2 <-> Way2
        GraphEdge.CreateDirect(Src2Node.Id, Way2Node.Id, 3f),
        GraphEdge.CreateDirect(Way2Node.Id, Src2Node.Id, 3f),

        // Dst1 <-> Way3
        GraphEdge.CreateDirect(Dst1Node.Id, Way3Node.Id),
        GraphEdge.CreateDirect(Way3Node.Id, Dst1Node.Id),

        // Dst2 <-> Way4
        GraphEdge.CreateDirect(Dst2Node.Id, Way4Node.Id, 3f),
        GraphEdge.CreateDirect(Way4Node.Id, Dst2Node.Id, 3f),

        // Prk1 <-> Way1
        GraphEdge.CreateDirect(Prk1Node.Id, Way1Node.Id),
        GraphEdge.CreateDirect(Way1Node.Id, Prk1Node.Id),

        // Prk2 <-> Way2
        GraphEdge.CreateDirect(Prk2Node.Id, Way2Node.Id, 3f),
        GraphEdge.CreateDirect(Way2Node.Id, Prk2Node.Id, 3f),

        // Crg1 <-> Way3
        GraphEdge.CreateDirect(Crg1Node.Id, Way3Node.Id),
        GraphEdge.CreateDirect(Way3Node.Id, Crg1Node.Id),

        // Crg2 <-> Way4
        GraphEdge.CreateDirect(Crg2Node.Id, Way4Node.Id, 3f),
        GraphEdge.CreateDirect(Way4Node.Id, Crg2Node.Id, 3f),

        // Way1 <-> Way2
        GraphEdge.CreateDirect(Way1Node.Id, Way2Node.Id),
        GraphEdge.CreateDirect(Way2Node.Id, Way1Node.Id),

        // Way2 <-> Way4
        GraphEdge.CreateDirect(Way2Node.Id, Way4Node.Id),
        GraphEdge.CreateDirect(Way4Node.Id, Way2Node.Id),

        // Way4 <-> Way3
        GraphEdge.CreateDirect(Way4Node.Id, Way3Node.Id),
        GraphEdge.CreateDirect(Way3Node.Id, Way4Node.Id),

        // Way3 <-> Way1
        GraphEdge.CreateDirect(Way3Node.Id, Way1Node.Id),
        GraphEdge.CreateDirect(Way1Node.Id, Way3Node.Id)
    };

    /// <summary>
    /// Generates a default agv graph
    /// </summary>
    public static AgvGraph CreateDefaultGraph(Guid? correlationId = null,
        ICollection<GraphNode>? nodes = null,
        ICollection<GraphEdge>? edges = null,
        AgvGraph.GraphUsability usability = AgvGraph.GraphUsability.Valid)
    {
        return new AgvGraph(correlationId ?? Guid.NewGuid(), nodes ?? Nodes, edges ?? Edges, usability);
    }

    public static AgvGraph CreateGraphWithAdditionalStationType(Guid? correlationId = null)
    {
        GraphNode[] nodes = [..Nodes, Double1];
        GraphEdge[] edges = [.. Edges, ..EdgesToDbl1];
        return new AgvGraph(correlationId ?? Guid.NewGuid(), nodes, edges);
    }

    public static AgvGraph CreateDefaultGraph(GraphNode[] nodes)
    {
        return new AgvGraph(Guid.NewGuid(), nodes, Edges);
    }

    public static AgvGraph Generate(IEnumerable<GraphNode> nodes,
        IEnumerable<GraphEdge> edges,
        Guid? correlationId = null,
        AgvGraph.GraphUsability usability = AgvGraph.GraphUsability.Valid)
    {
        return new AgvGraph(correlationId ?? Guid.NewGuid(), nodes.ToList(), edges.ToList(), usability);
    }

    public static GraphEdge CreateSlantedEdge(string fromNodeId, string toNodeId, float exitAngle, double exitDistance, GraphNode[] nodes)
    {
        var sourceNode = nodes.Single(n => n.Name == fromNodeId);
        var targetNode = nodes.Single(n => n.Name == toNodeId);
        double length = new Slanted(sourceNode.AsVector3(), targetNode.AsVector3(), exitAngle, exitDistance).GetLength();
        return GraphEdge.CreateSlanted(fromNodeId.ToGuid(), toNodeId.ToGuid(), (float) length, exitAngle, exitDistance);
    }

    public static GraphEdge CreatePolypathEdge(string fromNodeId, string toNodeId, List<EdgeCoefficient> pathPoints, GraphNode[] nodes)
    {
        var sourceNode = nodes.Single(n => n.Name == fromNodeId).AsVector3();
        var targetNode = nodes.Single(n => n.Name == toNodeId).AsVector3();
        List<Vector3> points = [sourceNode];
        points.AddRange(pathPoints.Select(c => new Vector3((float) c.X!, (float) c.Y!, 0f)));
        points.Add(targetNode);
        double length = new Polypath(points).GetLength();
        return GraphEdge.CreatePolypath(fromNodeId.ToGuid(), toNodeId.ToGuid(), (float) length, pathPoints);
    }

    public static Guid[] SelectRouteEdges(List<string> nodes, IReadOnlyCollection<GraphEdge> edges)
    {
        return nodes.Take(nodes.Count - 1).Select((id, index) => new { id, index })
            .Select(node => edges.FirstOrDefault(e => e.FromNodeId == node.id.ToGuid() && e.ToNodeId == nodes[node.index + 1].ToGuid()))
            .OfType<GraphEdge>().Select(edge => edge.Id).ToArray();
    }

    public static (GraphNode[] nodes, Guid[] edges) CreateRoute(GraphNode? targetNode = null)
    {
        var graph = CreateDefaultGraph();
        var edges = new List<Guid>();
        targetNode ??= Prk2.ToGraphNode();
        var nodes = new List<GraphNode> { targetNode };
        foreach (int unused in Enumerable.Range(0, 3))
        {
            var edge = graph.Edges.FirstOrDefault(e => e.ToNodeId == targetNode.Id && nodes.All(n => n.Id != e.FromNodeId));
            if (edge == null)
            {
                break;
            }
            string edgeName = Nodes.First(n => n.Id == edge.FromNodeId).Name;
            var fromNode = new GraphNode(edge.FromNodeId, edgeName);
            nodes.Add(fromNode);
            edges.Add(edge.Id);
            targetNode = fromNode;
        }
        nodes.Reverse();
        edges.Reverse();

        return (nodes.ToArray(), edges.ToArray());
    }

    public static ICollection<GraphNode> GetNodesOfDefaultGraph(ICollection<string> nodeNames)
    {
        var graph = CreateDefaultGraph();
        return graph.Nodes.Where(n => nodeNames.Contains(n.Name)).ToList();
    }

    public static ICollection<GraphNode> GetNodesOfDefaultGraph(ICollection<Guid> nodeIds)
    {
        var graph = CreateDefaultGraph();
        return graph.Nodes.Where(n => nodeIds.Contains(n.Id)).ToList();
    }
}

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global