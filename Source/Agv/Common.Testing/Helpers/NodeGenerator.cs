using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Testing.Helpers;

public static class NodeGenerator
{
    private static readonly Dictionary<string, string[]> _routeNodesByDestination = new(StringComparer.OrdinalIgnoreCase)
    {
        { "D2", new[] { "N003-001", "N003-002", "N004-002", "D2" } },
        { "D1", new[] { "N002-001", "N002-002", "N001-002", "D1" } }
    };

    public static GraphNode GenerateChargingNode(string nodeId = "CRG1")
    {
        return new GraphNode(nodeId) { Type = AgvGraph.NodeType.Charge, X = -2f, Y = -2f, Z = 0f };
    }

    public static GraphNode GenerateNode(AgvGraph.NodeType nodeType = AgvGraph.NodeType.WayPoint, float x = 1, float y = 2, float z = 0, float theta = 0f, string nodeId = "Way1")
    {
        return new GraphNode(nodeId) { Type = nodeType, X = x, Y = y, Z = z, Theta = theta };
    }

    public static GraphNode GenerateParkingNode(string nodeId = "PRK1")
    {
        return new GraphNode(nodeId) { Type = AgvGraph.NodeType.Park, X = -21f, Y = -21f, Z = 0f };
    }

    public static GraphNode GenerateNodeWithMultipleTypes(ICollection<AgvGraph.NodeType> nodeTypes, string nodeId = "PRK1")
    {
        return new GraphNode(nodeId) { Type = nodeTypes, X = -21f, Y = -21f, Z = 0f };
    }

    [UsedImplicitly]
    public static GraphNode GenerateMaintenanceNode(string nodeId = "MNT1")
    {
        return new GraphNode(nodeId) { Type = AgvGraph.NodeType.Park, X = 0f, Y = 0f, Z = 0f };
    }

    public static string[] GenerateRouteNodes(string destinationNode = "D2")
    {
        return _routeNodesByDestination[destinationNode];
    }
}