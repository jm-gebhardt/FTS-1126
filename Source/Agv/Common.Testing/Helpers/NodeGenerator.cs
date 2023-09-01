using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Testing.Helpers;

public static class NodeGenerator
{
    public static GraphNode GenerateChargingNode(string nodeId = "CRG1") => new(nodeId) {Type = AgvGraph.NodeType.Charge, X = -2f, Y = -2f, Z = 0f};

    public static GraphNode GenerateParkingNode(string nodeId = "PRK1") => new(nodeId) {Type = AgvGraph.NodeType.Park, X = -21f, Y = -21f, Z = 0f};

    [UsedImplicitly]
    public static GraphNode GenerateMaintenanceNode(string nodeId = "MNT1") => new(nodeId) {Type = AgvGraph.NodeType.Park, X = 0f, Y = 0f, Z = 0f};
}