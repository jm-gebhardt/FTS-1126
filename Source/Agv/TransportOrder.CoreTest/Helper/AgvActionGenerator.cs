using Gebhardt.StoreWare.Agv.Common.Messages.Actions;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;

public static class AgvActionGenerator
{
    public static AgvAction GenerateDropAction(string? targetNodeId = null)
    {
        var actionGroup = new ActionGroup(Guid.NewGuid(), "orderId", null); //temp need to populate actions with the actions generated here
        var dropAction = new AgvAction(Guid.NewGuid(), ActionType.Drop, actionGroup, targetNodeId ?? "Dest-1");
        return dropAction;
    }

    public static AgvAction GenerateChargeAction(string? targetNodeId = null)
    {
        var actionGroup = new ActionGroup(Guid.NewGuid(), "orderId", null); //temp need to populate actions with the actions generated here
        var chargeAction = new AgvAction(Guid.NewGuid(), ActionType.Charge, actionGroup, targetNodeId ?? "Charge-1");
        if (targetNodeId != null)
        {
            chargeAction.Nodes = new[] { targetNodeId };
        }
        return chargeAction;
    }

    public static AgvAction GenerateMaintenanceAction(string? targetNodeId = null)
    {
        var actionGroup = new ActionGroup(Guid.NewGuid(), "orderId", null); //temp need to populate actions with the actions generated here
        var maintenanceAction = new AgvAction(Guid.NewGuid(), ActionType.Move, actionGroup, targetNodeId ?? "Mnt-1");
        if (targetNodeId != null)
        {
            maintenanceAction.Nodes = new[] { targetNodeId };
        }
        return maintenanceAction;
    }

    public static AgvAction GenerateParkAction(string? targetNodeId = null)
    {
        var actionGroup = new ActionGroup(Guid.NewGuid(), "orderId", null); //temp need to populate actions with the actions generated here
        var parkAction = new AgvAction(Guid.NewGuid(), ActionType.Move, actionGroup, targetNodeId ?? "Prk-1");
        if (targetNodeId != null)
        {
            parkAction.Nodes = new[] { targetNodeId };
        }
        return parkAction;
    }

    public static ActionGroup GeneratePickDropActionGroup()
    {
        const string orderId = "orderId";
        var actionGroup = new ActionGroup(Guid.NewGuid(), orderId, null);

        var pickActionGuid = Guid.NewGuid();
        var dropActionGuid = Guid.NewGuid();
        var pickAction = new AgvAction(pickActionGuid, ActionType.Pick, actionGroup, "Src-1")
        {
            Successor = dropActionGuid,
            Sequence = 0
        };
        var dropAction = new AgvAction(dropActionGuid, ActionType.Drop, actionGroup, "Dst-1")
        {
            Predecessor = pickActionGuid,
            Sequence = 1
        };

        actionGroup.Actions = new List<AgvAction>
        {
            pickAction,
            dropAction
        };

        return actionGroup;
    }

    public static ActionGroup GenerateChargingActionGroup()
    {
        const string orderId = "orderId";
        var actionGroup = new ActionGroup(Guid.NewGuid(), orderId, null);

        var chargingActionGuid = Guid.NewGuid();
        var parkActionGuid = Guid.NewGuid();
        var chargingAction = new AgvAction(chargingActionGuid, ActionType.Charge, actionGroup, "Src-1")
        {
            Successor = parkActionGuid,
            Sequence = 0
        };
        var parkAction = new AgvAction(chargingActionGuid, ActionType.Move, actionGroup, "Src-1")
        {
            Predecessor = chargingActionGuid,
            Sequence = 1
        };

        actionGroup.Actions = new List<AgvAction>
        {
            chargingAction,
            parkAction
        };

        return actionGroup;
    }
}