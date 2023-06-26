using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;

namespace Gebhardt.StoreWare.Agv.Common.Testing.Helpers;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

public static class AgvGraphGenerator
{
    ///////// Defaut graph for unit tests /////////
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
    public static AgvGraph.Node[] Nodes { get; } = new[]
    {
        new AgvGraph.Node(Src1) { Enabled = true, Type = AgvGraph.NodeType.Pick,     X = 2f, Y = 2f, Z = 0f }, // 0
        new AgvGraph.Node(Src2) { Enabled = true, Type = AgvGraph.NodeType.Pick,     X = 3f, Y = 1f, Z = 0f }, // 1
        new AgvGraph.Node(Src3) { Enabled = true, Type = AgvGraph.NodeType.Pick,     X = 5f, Y = 1f, Z = 0f }, // 2 
        new AgvGraph.Node(Dst1) { Enabled = true, Type = AgvGraph.NodeType.Drop,     X = 2f, Y = 5f, Z = 0f }, // 3 
        new AgvGraph.Node(Dst2) { Enabled = true, Type = AgvGraph.NodeType.Drop,     X = 3f, Y = 6f, Z = 0f }, // 4
        new AgvGraph.Node(Dst3) { Enabled = true, Type = AgvGraph.NodeType.Drop,     X = 5f, Y = 6f, Z = 0f }, // 5 
        new AgvGraph.Node(Prk1) { Enabled = true, Type = AgvGraph.NodeType.Park,     X = 1f, Y = 3f, Z = 0f }, // 6 
        new AgvGraph.Node(Prk2) { Enabled = true, Type = AgvGraph.NodeType.Park,     X = 5f, Y = 3f, Z = 0f }, // 7 
        new AgvGraph.Node(Crg1) { Enabled = true, Type = AgvGraph.NodeType.Charge,   X = 1f, Y = 4f, Z = 0f }, // 8 
        new AgvGraph.Node(Crg2) { Enabled = true, Type = AgvGraph.NodeType.Charge,   X = 5f, Y = 4f, Z = 0f }, // 9 
        new AgvGraph.Node(Way1) { Enabled = true, Type = AgvGraph.NodeType.WayPoint, X = 2f, Y = 3f, Z = 0f }, // 10
        new AgvGraph.Node(Way2) { Enabled = true, Type = AgvGraph.NodeType.WayPoint, X = 3f, Y = 3f, Z = 0f }, // 11
        new AgvGraph.Node(Way3) { Enabled = true, Type = AgvGraph.NodeType.WayPoint, X = 2f, Y = 4f, Z = 0f }, // 12
        new AgvGraph.Node(Way4) { Enabled = true, Type = AgvGraph.NodeType.WayPoint, X = 3f, Y = 4f, Z = 0f }  // 13
    };

    public static AgvGraph.Node SourceClose            { get; } = Nodes[0];
    public static AgvGraph.Node SourceFar              { get; } = Nodes[1];
    public static AgvGraph.Node SourceUnreachable      { get; } = Nodes[2];
    public static AgvGraph.Node DestinationClose       { get; } = Nodes[3];
    public static AgvGraph.Node DestinationFar         { get; } = Nodes[4];
    public static AgvGraph.Node DestinationUnreachable { get; } = Nodes[5];
    public static AgvGraph.Node ParkClose              { get; } = Nodes[6];
    public static AgvGraph.Node ParkFar                { get; } = Nodes[7];
    public static AgvGraph.Node ChargingClose          { get; } = Nodes[8];
    public static AgvGraph.Node ChargingFar            { get; } = Nodes[9];
    public static AgvGraph.Node Waypoint1              { get; } = Nodes[10];
    public static AgvGraph.Node Waypoint2              { get; } = Nodes[11];
    public static AgvGraph.Node Waypoint3              { get; } = Nodes[12];
    public static AgvGraph.Node Waypoint4              { get; } = Nodes[13];

    /// <summary>
    /// Returns default edges
    /// </summary>
    public static AgvGraph.Edge[] Edges { get; }  = new[]
    {
        // Src1 <-> Way1
        new AgvGraph.Edge(Src1, Way1) { Metric = 1f },
        new AgvGraph.Edge(Way1, Src1) { Metric = 1f },

        // Src2 <-> Way2
        new AgvGraph.Edge(Src2, Way2) { Metric = 3f },
        new AgvGraph.Edge(Way2, Src2) { Metric = 3f },

        // Dst1 <-> Way3
        new AgvGraph.Edge(Dst1, Way3) { Metric = 1f },
        new AgvGraph.Edge(Way3, Dst1) { Metric = 1f },

        // Dst2 <-> Way4
        new AgvGraph.Edge(Dst2, Way4) { Metric = 3f },
        new AgvGraph.Edge(Way4, Dst2) { Metric = 3f },

        // Prk1 <-> Way1
        new AgvGraph.Edge(Prk1, Way1) { Metric = 1f },
        new AgvGraph.Edge(Way1, Prk1) { Metric = 1f },

        // Prk2 <-> Way2
        new AgvGraph.Edge(Prk2, Way2) { Metric = 3f },
        new AgvGraph.Edge(Way2, Prk2) { Metric = 3f },

        // Crg1 <-> Way3
        new AgvGraph.Edge(Crg1, Way3) { Metric = 1f },
        new AgvGraph.Edge(Way3, Crg1) { Metric = 1f },

        // Crg2 <-> Way4
        new AgvGraph.Edge(Crg2, Way4) { Metric = 3f },
        new AgvGraph.Edge(Way4, Crg2) { Metric = 3f },

        // Way1 <-> Way2
        new AgvGraph.Edge(Way1, Way2) { Metric = 1f },
        new AgvGraph.Edge(Way2, Way1) { Metric = 1f },

        // Way2 <-> Way4
        new AgvGraph.Edge(Way2, Way4) { Metric = 1f },
        new AgvGraph.Edge(Way4, Way2) { Metric = 1f },

        // Way4 <-> Way3
        new AgvGraph.Edge(Way4, Way3) { Metric = 1f },
        new AgvGraph.Edge(Way3, Way4) { Metric = 1f },

        // Way3 <-> Way1
        new AgvGraph.Edge(Way3, Way1) { Metric = 1f },
        new AgvGraph.Edge(Way1, Way3) { Metric = 1f }
    };

    /// <summary>
    /// Generates a default agv graph
    /// </summary>
    public static AgvGraph CreateDefaultGraph()
    {
        const string mapName = "Map#1";
        return new AgvGraph(Guid.Empty, Nodes, Edges, mapName.ToGuid(), "Map#1");
    }
}