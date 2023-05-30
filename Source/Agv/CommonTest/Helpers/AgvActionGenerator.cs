using Gebhardt.StoreWare.Agv.Common.Messages.Actions;

namespace Gebhardt.StoreWare.Agv.CommonTest.Helpers;

public static class AgvActionGenerator
{
    public static ActionGroup GenerateChargeActionGroup(string? targetNodeId = null)
    {
        const string orderId = "charge";
        var groupId = Guid.NewGuid();
        var chargeAction = new AgvAction(Guid.NewGuid(), ActionType.Charge, targetNodeId ?? "Charge-1", groupId);
        if (targetNodeId != null)
        {
            chargeAction.Nodes = new[] {targetNodeId};
        }

        var actionGroup = new ActionGroup(groupId, orderId, new List<AgvAction> {chargeAction})
        {
            State = ActionGroupState.Created
        };

        return actionGroup;
    }

    public static AgvAction GenerateParkAction(string? targetNodeId = null, Guid? parentId = null)
    {
        var parkAction = new AgvAction(Guid.NewGuid(), ActionType.Move, targetNodeId ?? "Prk-1", parentId ?? Guid.NewGuid());
        if (targetNodeId != null)
        {
            parkAction.Nodes = new[] {targetNodeId};
        }
        return parkAction;
    }

    public static ActionGroup GeneratePickDropActionGroup(ActionGroupState actionGroupState = ActionGroupState.Created)
    {
        const string orderId = "orderId";
        var groupId = Guid.NewGuid();

        var pickActionGuid = Guid.NewGuid();
        var dropActionGuid = Guid.NewGuid();
        var pickAction = new AgvAction(pickActionGuid, ActionType.Pick, "Src-1", groupId)
        {
            Sequence = 0
        };
        var dropAction = new AgvAction(dropActionGuid, ActionType.Drop, "Dst-1", groupId)
        {
            Sequence = 1
        };

        var actionGroup = new ActionGroup(groupId, orderId, new List<AgvAction> {pickAction, dropAction})
        {
            State = actionGroupState
        };

        return actionGroup;
    }

    public static ActionGroup GenerateActionGroup(IList<AgvAction> actions, ActionGroupState actionGroupState = ActionGroupState.Created)
    {
        var groupId = Guid.NewGuid();
        const string orderId = "orderId";
        
        return new ActionGroup(groupId, orderId, actions)
        {
            State = actionGroupState
        };
    }
}