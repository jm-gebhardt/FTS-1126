using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.Common.Testing.Helpers;

public static class NodeGenerator
{
    public static GraphNode GenerateChargingNode() => new("CRG1") {Type = AgvGraph.NodeType.Charge, X = -2f, Y = -2f, Z = 0f};

    public static GraphNode GenerateParkingNode() => new("PRK1") {Type = AgvGraph.NodeType.Park, X = -21f, Y = -21f, Z = 0f};

    public static GraphNode GenerateMaintenanceNode() => new("MNT1") {Type = AgvGraph.NodeType.Park, X = 0f, Y = 0f, Z = 0f};
}