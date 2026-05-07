using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using JetBrains.Annotations;
using static Gebhardt.Acc.Common.Domain.Models.Graphs.AgvGraph;

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
        return new GraphNode(nodeId.ToGuid(), nodeId) { Type = [NodeType.Charge], X = -2f, Y = -2f, Z = 0f };
    }

    public static GraphNode GenerateNode(NodeType nodeType = NodeType.WayPoint, float x = 1, float y = 2, float z = 0, float theta = 0f, string nodeName = "Way1", DateTime timeStamp = default)
    {
        if (timeStamp == default)
        {
            timeStamp = DateTime.UtcNow;
        }
        return new GraphNode(nodeName.ToGuid(), nodeName) { Type = [nodeType], X = x, Y = y, Z = z, Theta = theta, TimeStamp = timeStamp, Id = nodeName.ToGuid(), Lock = new NodeLock(){NodeId = nodeName.ToGuid() } };
    }

    public static GraphNode GenerateParkingNode(string nodeId = "PRK1")
    {
        return new GraphNode(nodeId.ToGuid(), nodeId) { Type = [NodeType.Park], X = 0f, Y = 0f, Z = 0f };
    }

    public static GraphNode GenerateNodeWithMultipleTypes(ICollection<NodeType> nodeTypes, string nodeId = "PRK1")
    {
        return new GraphNode(nodeId.ToGuid(), nodeId) { Type = nodeTypes, X = 0f, Y = 0f, Z = 0f };
    }

    public static GraphNode GenerateNodeWithTypeCollection(ICollection<NodeType> nodeTypes, float x = 1, float y = 2, float z = 0, float theta = 0f, string nodeName = "Way1", DateTime timeStamp = default)
    {
        if (timeStamp == default)
        {
            timeStamp = DateTime.UtcNow;
        }
        return new GraphNode(nodeName.ToGuid(), nodeName) { Type = nodeTypes, X = x, Y = y, Z = z, Theta = theta, TimeStamp = timeStamp, Id = nodeName.ToGuid(), Lock = new NodeLock() { NodeId = nodeName.ToGuid() } };
    }

    [UsedImplicitly]
    public static GraphNode GenerateMaintenanceNode(string nodeId = "MNT1")
    {
        return new GraphNode(nodeId.ToGuid(), nodeId) { Type = [NodeType.Park], X = 0f, Y = 0f, Z = 0f };
    }

    public static GraphNode[] GenerateRouteNodes(string destinationNode = "D2")
    {
        return _routeNodesByDestination[destinationNode].Select(n => new GraphNode(n.ToGuid(), n)).ToArray();
    }
}