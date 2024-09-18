using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Testing.Helpers;

public static class AgvActionGenerator
{
    private static string[] GetNodes(string startNodeId, string targetNodeId, int numberOfNodes, IEnumerable<GraphNode>? availableNodes = null)
    {
        var nodes = new List<string>
        {
            startNodeId
        };

        if (startNodeId == targetNodeId)
        {
            return nodes.ToArray();
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
                nodes.Add(graphNodes.ElementAt(i % graphNodes.Count).Id);
            }
        }
        nodes.Add(targetNodeId);

        return nodes.ToArray();
    }

    [UsedImplicitly]
    public static ActionGroup GenerateChargeActionGroup(
        ActionGroupState actionGroupState = ActionGroupState.Created, ActionState actionState = ActionState.Created, string? targetNodeId = null,
        Guid? assignedAgv = null)
    {
        var groupId = Guid.NewGuid();

        var moveActionGuid = Guid.NewGuid();
        var chargeActionGuid = Guid.NewGuid();
        var moveAction = new AgvAction(moveActionGuid, ActionType.Move, targetNodeId ?? "Charge-1", groupId)
        {
            Sequence = 0,
            State = actionState
        };
        var chargeAction = new AgvAction(chargeActionGuid, ActionType.Charge, targetNodeId ?? "Charge-1", groupId)
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
        var action = new AgvAction(Guid.NewGuid(), ActionType.Charge, targetNodeId, parentId ?? Guid.NewGuid())
        {
            Nodes = new[]
            {
                targetNodeId
            }
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
        var action = new AgvAction(Guid.NewGuid(), actionType, targetNodeId, parentId ?? Guid.NewGuid())
        {
            Nodes = GetNodes(startNodeId, targetNodeId, numberOfNodes, availableNodes)
        };

        return action;
    }

    public static ActionGroup GeneratePickDropActionGroup(
        ActionGroupState actionGroupState = ActionGroupState.Created,
        ActionState actionState = ActionState.Created,
        string? source = null,
        string? destination = null,
        Guid? assignedAgv = null)
    {
        const string orderId = "orderId-pick-drop";
        var groupId = Guid.NewGuid();

        var pickActionGuid = Guid.NewGuid();
        var dropActionGuid = Guid.NewGuid();
        var pickAction = new AgvAction(pickActionGuid, ActionType.Pick, source ?? AgvGraphGenerator.SourceClose.Id, groupId)
        {
            Sequence = 0,
            State = actionState
        };
        var dropAction = new AgvAction(dropActionGuid, ActionType.Drop, destination ?? AgvGraphGenerator.DestinationClose.Id, groupId)
        {
            Sequence = 1,
            State = actionState
        };

        var actionGroup = new ActionGroup(groupId, orderId, new List<AgvAction> { pickAction, dropAction }, ActionGroupCreator.Wms)
        {
            State = actionGroupState,
            AssignedAgv = assignedAgv
        };

        return actionGroup;
    }

    [UsedImplicitly]
    public static ActionGroup GenerateIncompleteActionGroupWithoutPickTarget(ActionGroupState actionGroupState = ActionGroupState.Created,
        ActionState actionState = ActionState.Created,
        string? moveDestination = null,
        string? destination = null,
        Guid? assignedAgv = null)
    {
        const string orderId = "orderId-pick-drop";
        var groupId = Guid.NewGuid();

        var pickActionGuid = Guid.NewGuid();
        var dropActionGuid = Guid.NewGuid();
        var moveActionGuid = Guid.NewGuid();

        var moveAction = new AgvAction(moveActionGuid, ActionType.Move, moveDestination ?? AgvGraphGenerator.Waypoint3.Id, groupId)
        {
            Sequence = 0,
            State = actionState
        };

        var pickAction = new AgvAction(pickActionGuid, ActionType.Pick,string.Empty, groupId)
        {
            Sequence = 1,
            State = actionState
        };
        var dropAction = new AgvAction(dropActionGuid, ActionType.Drop, destination ?? AgvGraphGenerator.DestinationClose.Id, groupId)
        {
            Sequence = 2,
            State = actionState
        };

        var actionGroup = new ActionGroup(groupId, orderId, new List<AgvAction> { moveAction, pickAction, dropAction }, ActionGroupCreator.Wms)
        {
            State = actionGroupState,
            AssignedAgv = assignedAgv
        };

        return actionGroup;
    }

    [UsedImplicitly]
    public static ActionGroup GenerateIncompleteActionGroupWithoutDropTarget(ActionGroupState actionGroupState = ActionGroupState.Created,
        ActionState actionState = ActionState.Created,
        string? source = null,
        string? moveDestination = null,
        Guid? assignedAgv = null)
    {
        const string orderId = "orderId-pick-drop";
        var groupId = Guid.NewGuid();

        var pickActionGuid = Guid.NewGuid();
        var dropActionGuid = Guid.NewGuid();
        var moveActionGuid = Guid.NewGuid();

        var pickAction = new AgvAction(pickActionGuid, ActionType.Pick, source ?? AgvGraphGenerator.SourceClose.Id, groupId)
        {
            Sequence = 0,
            State = actionState
        };

        var moveAction = new AgvAction(moveActionGuid, ActionType.Move, moveDestination ?? AgvGraphGenerator.Waypoint3.Id, groupId)
        {
            Sequence = 1,
            State = actionState
        };

        var dropAction = new AgvAction(dropActionGuid, ActionType.Drop, string.Empty, groupId)
        {
            Sequence = 2,
            State = actionState
        };

        var actionGroup = new ActionGroup(groupId, orderId, new List<AgvAction> { pickAction, moveAction, dropAction }, ActionGroupCreator.Wms)
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
        var moveAction = new AgvAction(Guid.NewGuid(), ActionType.Move, targetNodeId ?? "Move-1", groupId);
        if (targetNodeId != null)
        {
            moveAction.Nodes = new[] { targetNodeId };
        }

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