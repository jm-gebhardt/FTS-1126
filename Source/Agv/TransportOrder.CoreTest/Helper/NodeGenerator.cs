using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;

public static class NodeGenerator
{
    public static AgvGraph.Node GenerateChargingNode() => new("CRG1") { Enabled = true, Type = AgvGraph.NodeType.Charge, X = -2f, Y = -2f, Z = 1f };

    public static AgvGraph.Node GenerateParkingNode() => new("PRK1") { Enabled = true, Type = AgvGraph.NodeType.Park, X = -21f, Y = -21f, Z = 10f };

    public static AgvGraph.Node GenerateMaintenanceNode() => new("MNT1") { Enabled = true, Type = AgvGraph.NodeType.Park, X = 0f, Y = 0f, Z = 0f };
}