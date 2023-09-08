using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Testing.Helpers;

public static class AgvActionGenerator
{
    [UsedImplicitly]
    public static ActionGroup GenerateChargeActionGroup(string? targetNodeId = null)
    {
        const string orderId = "charge";
        var groupId = Guid.NewGuid();
        var chargeAction = new AgvAction(Guid.NewGuid(), ActionType.Charge, targetNodeId ?? "Charge-1", groupId);
        if (targetNodeId != null)
        {
            chargeAction.Nodes = new[] {targetNodeId};
        }

        var actionGroup = new ActionGroup(groupId, orderId, new List<AgvAction> {chargeAction}, ActionGroupCreator.TransportOrder)
        {
            State = ActionGroupState.Created
        };

        return actionGroup;
    }

    [UsedImplicitly]
    public static AgvAction GenerateParkAction(string? targetNodeId = null, Guid? parentId = null)
    {
        var parkAction = new AgvAction(Guid.NewGuid(), ActionType.Move, targetNodeId ?? "Prk-1", parentId ?? Guid.NewGuid());
        if (targetNodeId != null)
        {
            parkAction.Nodes = new[] {targetNodeId};
        }
        return parkAction;
    }

    public static ActionGroup GeneratePickDropActionGroup(
        ActionGroupState actionGroupState = ActionGroupState.Created,
        ActionState actionState = ActionState.Created,
        string? source = null,
        string? destination = null,
        Guid? assignedAgv = null)
    {
        const string orderId = "orderId";
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
        const string orderId = "move";
        var groupId = Guid.NewGuid();
        var moveAction = new AgvAction(Guid.NewGuid(), ActionType.Move, targetNodeId ?? "Move-1", groupId);
        if (targetNodeId != null)
        {
            moveAction.Nodes = new[] {targetNodeId};
        }

        var actionGroup = new ActionGroup(groupId, orderId, new List<AgvAction> {moveAction}, isManual:isManual, creator:ActionGroupCreator.TransportOrderWebApi)
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