using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Testing.Helpers;

[PublicAPI]
public static class AgvActionGenerator
{
    private static GraphNode[] GetNodes(string startNodeId, string targetNodeId, int numberOfNodes, IEnumerable<GraphNode>? availableNodes = null)
    {
        var nodes = new List<string>
        {
            startNodeId
        };

        if (startNodeId == targetNodeId)
        {
            return nodes.Select(n => new GraphNode(n.ToGuid(), n)).ToArray();
        }

        int nodesToBeAdded = numberOfNodes - 2;
        if (nodesToBeAdded > 0)
        {
            List<GraphNode>? graphNodes = availableNodes?.ToList();
            if (graphNodes == null)
            {
                throw new ArgumentException("availableNodeIds must be provided if numberOfNodes > 2");
            }
            for (int i = 0; i < nodesToBeAdded; i++)
            {
                // take element based on index, if it exceeds start from beginning
                nodes.Add(graphNodes.ElementAt(i % graphNodes.Count).Name);
            }
        }
        nodes.Add(targetNodeId);

        return nodes.Select(n => new GraphNode(n.ToGuid(), n)).ToArray();
    }

    [UsedImplicitly]
    public static ActionGroup GenerateChargeActionGroup(
        ActionGroupState actionGroupState = ActionGroupState.Created, ActionState actionState = ActionState.Created, string? targetNodeId = null,
        Guid? assignedAgv = null)
    {
        var groupId = Guid.NewGuid();

        var moveActionGuid = Guid.NewGuid();
        var chargeActionGuid = Guid.NewGuid();
        var moveAction = new AgvAction(moveActionGuid, ActionType.Move, (targetNodeId ?? "Charge-1").ToGraphNode(), groupId)
        {
            Sequence = 0,
            State = actionState
        };
        var chargeAction = new AgvAction(chargeActionGuid, ActionType.Charge, (targetNodeId ?? "Charge-1").ToGraphNode(), groupId)
        {
            Sequence = 1,
            State = actionState
        };

        var actionGroup = new ActionGroup(groupId, string.Empty, new List<AgvAction> { moveAction, chargeAction }, ActionGroupCreator.TransportOrder)
        {
            State = actionGroupState,
            AssignedAgv = assignedAgv
        };

        return actionGroup;
    }

    [UsedImplicitly]
    public static AgvAction GenerateChargeAction(string startNodeId = "CRG1", string targetNodeId = "CRG1", Guid? parentId = null)
    {
        var action = new AgvAction(Guid.NewGuid(), ActionType.Charge, targetNodeId.ToGraphNode(), parentId ?? Guid.NewGuid())
        {
            Nodes =
            [
                new GraphNode(targetNodeId.ToGuid(), targetNodeId)
            ]
        };

        return action;
    }

    /// <summary>
    /// Generates a move action with the given parameters.
    /// </summary>
    /// <param name="startNodeId"></param>
    /// <param name="availableNodes"></param>
    /// <param name="numberOfNodes">Creates this number of nodes in total in the action. The nodes in between are taken from the availableNodes parameter</param>
    /// <param name="targetNodeId"></param>
    /// <param name="parentId"></param>
    /// <returns></returns>
    public static AgvAction GenerateMoveAction(string startNodeId = "N1", IEnumerable<GraphNode>? availableNodes = null, int numberOfNodes = 2, string targetNodeId = "Move-1", Guid? parentId = null)
    {
        return GenerateAction(startNodeId, ActionType.Move, availableNodes, numberOfNodes, targetNodeId, parentId);
    }

    /// <summary>
    /// Generates an action with the given parameters.
    /// </summary>
    /// <param name="startNodeId"></param>
    /// <param name="actionType"></param>
    /// <param name="availableNodes"></param>
    /// <param name="numberOfNodes">Creates this number of nodes in total in the action. The nodes in between are taken from the availableNodes parameter</param>
    /// <param name="targetNodeId"></param>
    /// <param name="parentId"></param>
    /// <returns></returns>
    public static AgvAction GenerateAction(string startNodeId = "N1", ActionType actionType = ActionType.Move, IEnumerable<GraphNode>? availableNodes = null, int numberOfNodes = 2, string targetNodeId = "Move-1", Guid? parentId = null)
    {
        var action = new AgvAction(Guid.NewGuid(), actionType, targetNodeId.ToGraphNode(), parentId ?? Guid.NewGuid())
        {
            Nodes = GetNodes(startNodeId, targetNodeId, numberOfNodes, availableNodes)
        };

        return action;
    }

    public static ActionGroup GeneratePickDropActionGroup(
        Guid? groupId = null,
        ActionGroupState actionGroupState = ActionGroupState.Created,
        ActionState actionState = ActionState.Created,
        string? source = null,
        string? destination = null,
        Guid? assignedAgv = null,
        string? hu = null,
        string? additionalPickInfo = null,
        bool isQueued = true)
    {
        const string orderId = "orderId-pick-drop";
        groupId ??= Guid.NewGuid();
        var pickActionGuid = Guid.NewGuid();
        var dropActionGuid = Guid.NewGuid();
        var pickAction = new AgvAction(pickActionGuid, ActionType.Pick, (source ?? AgvGraphGenerator.SourceClose.Name).ToGraphNode(), groupId.Value)
        {
            Sequence = 0,
            State = actionState,
            AdditionalInfo = additionalPickInfo
        };
        var dropAction = new AgvAction(dropActionGuid, ActionType.Drop, (destination ?? AgvGraphGenerator.DestinationClose.Name).ToGraphNode(), groupId.Value)
        {
            Sequence = 1,
            State = actionState
        };

        var actionGroup = new ActionGroup(groupId.Value, orderId, new List<AgvAction> { pickAction, dropAction }, ActionGroupCreator.Wms)
        {
            State = actionGroupState,
            AssignedAgv = assignedAgv,
            HandlingUnit = hu,
            IsQueued = isQueued
        };

        return actionGroup;
    }

    public static ActionGroup GenerateMovePickDropActionGroup(
        Guid? groupId = null,
        ActionGroupState actionGroupState = ActionGroupState.Created,
        ActionState actionState = ActionState.Created,
        string? source = null,
        string? destination = null,
        Guid? assignedAgv = null,
        string? hu = null,
        string? additionalPickInfo = null,
        bool isQueued = true)
    {
        const string orderId = "orderId-pick-drop";
        groupId ??= Guid.NewGuid();
        var moveActionGuid = Guid.NewGuid();
        var pickActionGuid = Guid.NewGuid();
        var dropActionGuid = Guid.NewGuid();
        var moveAction = new AgvAction(moveActionGuid, ActionType.Move, AgvGraphGenerator.Waypoint3, groupId.Value)
        {
            Sequence = 0,
            State = actionState
        };
        var pickAction = new AgvAction(pickActionGuid, ActionType.Pick, source != null ? source.ToGraphNode() : AgvGraphGenerator.SourceClose, groupId.Value)
        {
            Sequence = 1,
            State = actionState,
            AdditionalInfo = additionalPickInfo
        };
        var dropAction = new AgvAction(dropActionGuid, ActionType.Drop, destination != null ? destination.ToGraphNode() : AgvGraphGenerator.DestinationClose, groupId.Value)
        {
            Sequence = 2,
            State = actionState
        };

        var actionGroup = new ActionGroup(groupId.Value, orderId, new List<AgvAction> { moveAction, pickAction, dropAction }, ActionGroupCreator.Wms)
        {
            State = actionGroupState,
            AssignedAgv = assignedAgv,
            HandlingUnit = hu,
            IsQueued = isQueued
        };

        return actionGroup;
    }

    public static ActionGroup GeneratePickMoveDropActionGroup(
        Guid? groupId = null,
        ActionGroupState actionGroupState = ActionGroupState.Created,
        ActionState actionState = ActionState.Created,
        string? source = null,
        string? destination = null,
        Guid? assignedAgv = null,
        string? hu = null,
        string? additionalPickInfo = null,
        bool isQueued = true)
    {
        const string orderId = "orderId-pick-drop";
        groupId ??= Guid.NewGuid();
        var pickActionGuid = Guid.NewGuid();
        var moveActionGuid = Guid.NewGuid();
        var dropActionGuid = Guid.NewGuid();
        var pickAction = new AgvAction(pickActionGuid, ActionType.Pick, source != null ? source.ToGraphNode() : AgvGraphGenerator.SourceClose, groupId.Value)
        {
            Sequence = 0,
            State = actionState,
            AdditionalInfo = additionalPickInfo
        };
        var moveAction = new AgvAction(moveActionGuid, ActionType.Move, AgvGraphGenerator.Waypoint3, groupId.Value)
        {
            Sequence = 1,
            State = actionState
        };
        var dropAction = new AgvAction(dropActionGuid, ActionType.Drop, destination != null ? destination.ToGraphNode() : AgvGraphGenerator.DestinationClose, groupId.Value)
        {
            Sequence = 2,
            State = actionState
        };

        var actionGroup = new ActionGroup(groupId.Value, orderId, new List<AgvAction> { moveAction, pickAction, dropAction }, ActionGroupCreator.Wms)
        {
            State = actionGroupState,
            AssignedAgv = assignedAgv,
            HandlingUnit = hu,
            IsQueued = isQueued
        };

        return actionGroup;
    }

    [UsedImplicitly]
    public static ActionGroup GenerateIncompleteActionGroupWithoutPickTarget(
        Guid? groupId = null,
        ActionGroupState actionGroupState = ActionGroupState.Created,
        ActionState actionState = ActionState.Created,
        string? moveDestination = null,
        string? destination = null,
        Guid? assignedAgv = null)
    {
        const string orderId = "orderId-pick-drop";
        groupId ??= Guid.NewGuid();

        var pickActionGuid = Guid.NewGuid();
        var dropActionGuid = Guid.NewGuid();
        var moveActionGuid = Guid.NewGuid();

        var moveAction = new AgvAction(moveActionGuid, ActionType.Move, moveDestination != null ? moveDestination.ToGraphNode() : AgvGraphGenerator.Waypoint3, groupId.Value)
        {
            Sequence = 0,
            State = actionState
        };

        var pickAction = new AgvAction(pickActionGuid, ActionType.Pick, targetNode: null, groupId.Value)
        {
            Sequence = 1,
            State = actionState
        };
        var dropAction = new AgvAction(dropActionGuid, ActionType.Drop, destination != null ? destination.ToGraphNode() : AgvGraphGenerator.DestinationClose, groupId.Value)
        {
            Sequence = 2,
            State = actionState
        };

        var actionGroup = new ActionGroup(groupId.Value, orderId, new List<AgvAction> { moveAction, pickAction, dropAction }, ActionGroupCreator.Wms)
        {
            State = actionGroupState,
            AssignedAgv = assignedAgv
        };

        return actionGroup;
    }

    [UsedImplicitly]
    public static ActionGroup GenerateIncompleteActionGroupWithoutDropTarget(
        Guid? groupId = null,
        ActionGroupState actionGroupState = ActionGroupState.Created,
        ActionState actionState = ActionState.Created,
        string? source = null,
        string? moveDestination = null,
        Guid? assignedAgv = null)
    {
        const string orderId = "orderId-pick-drop";
        groupId ??= Guid.NewGuid();

        var pickActionGuid = Guid.NewGuid();
        var dropActionGuid = Guid.NewGuid();
        var moveActionGuid = Guid.NewGuid();

        var pickAction = new AgvAction(pickActionGuid, ActionType.Pick, source != null ? source.ToGraphNode() : AgvGraphGenerator.SourceClose, groupId.Value)
        {
            Sequence = 0,
            State = actionState
        };

        var moveAction = new AgvAction(moveActionGuid, ActionType.Move, moveDestination != null ? moveDestination.ToGraphNode() : AgvGraphGenerator.Waypoint3, groupId.Value)
        {
            Sequence = 1,
            State = actionState
        };

        var dropAction = new AgvAction(dropActionGuid, ActionType.Drop, targetNode: null, groupId.Value)
        {
            Sequence = 2,
            State = actionState
        };

        var actionGroup = new ActionGroup(groupId.Value, orderId, new List<AgvAction> { pickAction, moveAction, dropAction }, ActionGroupCreator.Wms)
        {
            State = actionGroupState,
            AssignedAgv = assignedAgv
        };

        return actionGroup;
    }

    [UsedImplicitly]
    public static ActionGroup GenerateMoveActionGroup(string? targetNodeId = null, bool isManual = false)
    {
        const string orderId = "orderId-move";
        var groupId = Guid.NewGuid();
        var moveAction = new AgvAction(Guid.NewGuid(), ActionType.Move, (targetNodeId ?? "Move-1").ToGraphNode(), groupId);
        if (targetNodeId != null)
        {
            moveAction.Nodes = [new GraphNode(targetNodeId.ToGuid(), targetNodeId)];
        }

        var actionGroup = new ActionGroup(groupId, orderId, new List<AgvAction> { moveAction }, isManual: isManual, creator: ActionGroupCreator.TransportOrderWebApi)
        {
            State = ActionGroupState.Created
        };

        return actionGroup;
    }

    [UsedImplicitly]
    public static ActionGroup GenerateMoveToCoordinateActionGroup(string? targetNodeId = null, bool isManual = false)
    {
        const string orderId = "orderId-move";
        var groupId = Guid.NewGuid();
        var moveAction = new AgvAction() { ActionType = ActionType.Move, CorrelationId = Guid.NewGuid(), Target = new ActionTarget(1, 1, 0) };

        var actionGroup = new ActionGroup(groupId, orderId, new List<AgvAction> { moveAction }, isManual: isManual, creator: ActionGroupCreator.TransportOrderWebApi)
        {
            State = ActionGroupState.Created
        };

        return actionGroup;
    }

    public static ActionGroup GenerateActionGroup(IList<AgvAction> actions, ActionGroupState actionGroupState = ActionGroupState.Created)
    {
        var groupId = Guid.NewGuid();
        const string orderId = "orderId";

        return new ActionGroup(groupId, orderId, actions, ActionGroupCreator.Wms)
        {
            State = actionGroupState
        };
    }
}