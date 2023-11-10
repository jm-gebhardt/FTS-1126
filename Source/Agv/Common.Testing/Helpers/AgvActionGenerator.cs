using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Testing.Helpers;

public static class AgvActionGenerator
{
    private static string[] GetNodes(string startNodeId, string targetNodeId, int numberOfNodes, IList<GraphNode>? availableNodes = null)
    {
        var nodes = new List<string>
        {
            startNodeId
        };

        int nodesToBeAdded = numberOfNodes - 2;
        if (nodesToBeAdded > 0)
        {
            if (availableNodes == null)
            {
                throw new ArgumentException("availableNodeIds must be provided if numberOfNodes > 2");
            }
            for (int i = 0; i < nodesToBeAdded; i++)
            {
                // take element based on index, if it exceeds start from beginning
                nodes.Add(availableNodes.ElementAt(i % availableNodes.Count).Id);
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

        var actionGroup = new ActionGroup(groupId, string.Empty, new List<AgvAction> {moveAction, chargeAction}, ActionGroupCreator.TransportOrder)
        {
            State = actionGroupState,
            AssignedAgv = assignedAgv
        };

        return actionGroup;
    }

    [UsedImplicitly]
    public static AgvAction GenerateChargeAction(string startNodeId = "N1", string targetNodeId = "C1", Guid? parentId = null)
    {
        var action = new AgvAction(Guid.NewGuid(), ActionType.Charge, targetNodeId, parentId ?? Guid.NewGuid())
        {
            Nodes = new[]
            {
                startNodeId,
                targetNodeId
            }
        };

        return action;
    }

    public static AgvAction GenerateMoveAction(string startNodeId = "N1", IList<GraphNode>? availableNodes = null, int numberOfNodes = 2, string targetNodeId = "Move-1", Guid? parentId = null)
    {
        var action = new AgvAction(Guid.NewGuid(), ActionType.Move, targetNodeId, parentId ?? Guid.NewGuid())
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

        var actionGroup = new ActionGroup(groupId, orderId, new List<AgvAction> {pickAction, dropAction}, ActionGroupCreator.Wms)
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
            moveAction.Nodes = new[] {targetNodeId};
        }

        var actionGroup = new ActionGroup(groupId, orderId, new List<AgvAction> {moveAction}, isManual: isManual, creator: ActionGroupCreator.TransportOrderWebApi)
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