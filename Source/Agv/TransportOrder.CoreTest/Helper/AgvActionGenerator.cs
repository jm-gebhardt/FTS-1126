using Gebhardt.StoreWare.Agv.Common.Messages.Actions;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;

public static class AgvActionGenerator
{
    public static AgvAction GenerateDropAction(string? targetNodeId = null, Guid? parentId = null)
    {
        var dropAction = new AgvAction(Guid.NewGuid(), ActionType.Drop, targetNodeId ?? "Dest-1", parentId ?? Guid.NewGuid());
        return dropAction;
    }

    public static AgvAction GenerateChargeAction(string? targetNodeId = null, Guid? parentId = null)
    {
        var chargeAction = new AgvAction(Guid.NewGuid(), ActionType.Charge, targetNodeId ?? "Charge-1", parentId ?? Guid.NewGuid());
        if (targetNodeId != null)
        {
            chargeAction.Nodes = new[] {targetNodeId};
        }
        return chargeAction;
    }

    public static AgvAction GenerateMaintenanceAction(string? targetNodeId = null, Guid? parentId = null)
    {
        var maintenanceAction = new AgvAction(Guid.NewGuid(), ActionType.Move, targetNodeId ?? "Mnt-1", parentId ?? Guid.NewGuid());
        if (targetNodeId != null)
        {
            maintenanceAction.Nodes = new[] {targetNodeId};
        }
        return maintenanceAction;
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

    public static ActionGroup GenerateChargingActionGroup()
    {
        const string orderId = "orderId";
        var groupId = Guid.NewGuid();

        var chargingActionGuid = Guid.NewGuid();
        var chargingAction = new AgvAction(chargingActionGuid, ActionType.Charge, "Src-1", groupId)
        {
            Sequence = 0
        };
        var parkAction = new AgvAction(chargingActionGuid, ActionType.Move, "Src-1", groupId)
        {
            Sequence = 1
        };
        var actionGroup = new ActionGroup(groupId, orderId, new List<AgvAction> {chargingAction, parkAction});

        return actionGroup;
    }
}