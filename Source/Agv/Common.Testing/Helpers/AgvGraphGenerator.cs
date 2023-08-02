using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;

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

    private const string Src1 = "SRC1"; // Source close
    private const string Src2 = "SRC2"; // Source far
    private const string Src3 = "SRC3"; // Source unreachable
    private const string Dst1 = "DST1"; // Destination close
    private const string Dst2 = "DST2"; // Destination far
    private const string Dst3 = "DST3"; // Destination unreachable
    private const string Prk1 = "PRK1"; // Parking spot close
    private const string Prk2 = "PRK2"; // Parking spot far
    private const string Crg1 = "CRG1"; // Charging point close
    private const string Crg2 = "CRG2"; // Charging point far
    private const string Way1 = "WAY1"; // Waypoint 1
    private const string Way2 = "WAY2"; // Waypoint 2
    private const string Way3 = "WAY3"; // Waypoint 3
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
        new(Src1, Way1, 1f),
        new(Way1, Src1, 1f),

        // Src2 <-> Way2
        new(Src2, Way2, 3f),
        new(Way2, Src2, 3f),

        // Dst1 <-> Way3
        new(Dst1, Way3, 1f),
        new(Way3, Dst1, 1f),

        // Dst2 <-> Way4
        new(Dst2, Way4, 3f),
        new(Way4, Dst2, 3f),

        // Prk1 <-> Way1
        new(Prk1, Way1, 1f),
        new(Way1, Prk1, 1f),

        // Prk2 <-> Way2
        new(Prk2, Way2, 3f),
        new(Way2, Prk2, 3f),

        // Crg1 <-> Way3
        new(Crg1, Way3, 1f),
        new(Way3, Crg1, 1f),

        // Crg2 <-> Way4
        new(Crg2, Way4, 3f),
        new(Way4, Crg2, 3f),

        // Way1 <-> Way2
        new(Way1, Way2, 1f),
        new(Way2, Way1, 1f),

        // Way2 <-> Way4
        new(Way2, Way4, 1f),
        new(Way4, Way2, 1f),

        // Way4 <-> Way3
        new(Way4, Way3, 1f),
        new(Way3, Way4, 1f),

        // Way3 <-> Way1
        new(Way3, Way1, 1f),
        new(Way1, Way3, 1f)
    };

    /// <summary>
    /// Generates a default agv graph
    /// </summary>
    public static AgvGraph CreateDefaultGraph(Guid? correlationId = null)
    {
        const string mapName = "Map#1";
        return new AgvGraph(correlationId ?? Guid.NewGuid(), Nodes, Edges, mapName.ToGuid(), "Map#1");
    }
    
    public static AgvGraph CreateDefaultGraph(GraphNode[] nodes)
    {
        const string mapName = "Map#1";
        return new AgvGraph(Guid.NewGuid(), nodes, Edges, mapName.ToGuid(), "Map#1");
    }
    
}