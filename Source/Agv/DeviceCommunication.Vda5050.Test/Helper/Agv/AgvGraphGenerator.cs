using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;

/// <summary>
/// Provides a helper to generate <see cref="AgvGraph"/> components for unit testing.
/// </summary>
public static class AgvGraphGenerator
{
    public static AgvGraph GenerateFromNodeIds(string[] nodeIds, Dictionary<string, ICollection<AgvNodeAction>> nodeActions, Guid? agvCorrelationId=null)
    {
        List<Node> nodes = [];
        List<Edge> edges = [];
        string previousNode = string.Empty;
        foreach (var nodeId in nodeIds.Select((value, index) => new { value, index }))
        {
            nodeActions.TryGetValue(nodeId.value, out ICollection<AgvNodeAction>? actions);
            var node = GenerateNode(nodeId.value,actions: actions );
            nodes.Add(node);
            if (nodeId.index > 0)
            {
                edges.Add(GenerateEdge(startNodeId: previousNode, endNodeId: node.NodeId));
            }
            previousNode = node.NodeId;
        }
        return new AgvGraph(agvCorrelationId ?? Guid.NewGuid(), nodes, edges);
    }

    public static Edge GenerateEdge(string startNodeId="start", string endNodeId="end", ICollection<AgvEdgeAction>?actions=null)
    {
        return new Edge
        {
            EdgeId = "edge",
            Id = Guid.NewGuid(),
            StartNodeId = startNodeId,
            EndNodeId = endNodeId,
            AgvActions = actions ?? [],
            AgvTrajectory = new Trajectory(),
            AgvOrientation = 0.5,
            OrientationType = OrientationType.Global,
            RotationAllowed = true,
            MaxSpeed = 1,
            MaxHeight = 2,
            MaxRotationSpeed = 1,
            MinHeight = 0.5
        };
    }

    public static Node GenerateNode(string nodeId="node", string mapId="map", ICollection<AgvNodeAction>? actions = null)
    {
        return new Node
        {
            Id = nodeId.ToGuid(),
            NodeId = nodeId,
            Position = new NodePosition(1,1),
            MapId = mapId,
            AgvActions = actions ?? []
        };
    }

    public static AgvNodeAction GenerateNodeAction(string actionType="type", Guid? agvCorrelationId=null, RequirementType requirementType=RequirementType.Required, BlockingType blockingType=BlockingType.NONE, ICollection<OrderActionParameter>? parameters=null)
    {
        return new AgvNodeAction()
        {
            ActionType = actionType,
            AgvCorrelationId = agvCorrelationId ?? Guid.NewGuid(),
            BlockingType = blockingType,
            Parameters = parameters ?? [],
            RequirementType = requirementType
        };
    }

    public static AgvEdgeAction GenerateEdgeAction(string actionType = "waitForTrigger", Guid? agvCorrelationId = null, RequirementType requirementType = RequirementType.Required, BlockingType blockingType = BlockingType.NONE, ICollection<OrderActionParameter>? parameters = null)
    {
        return new AgvEdgeAction()
        {
            ActionType = actionType,
            AgvCorrelationId = agvCorrelationId ?? Guid.NewGuid(),
            BlockingType = blockingType,
            Parameters = parameters ?? [],
            RequirementType = requirementType
        };
    }
}
