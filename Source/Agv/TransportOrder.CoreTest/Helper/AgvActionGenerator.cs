using Gebhardt.StoreWare.Agv.Common.Messages.Actions;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;

public static class AgvActionGenerator
{
    public static AgvAction GenerateDropAction()
    {
        var dropAction = new AgvAction(Guid.NewGuid(), string.Empty, ActionType.Drop, "orderId", Guid.NewGuid(), "Dest-1");
        return dropAction;
    }
    
    public static AgvAction GenerateChargeAction()
    {
        var chargeAction = new AgvAction(Guid.NewGuid(), string.Empty, ActionType.Charge, "orderId", Guid.NewGuid(), "Charge-1");
        return chargeAction;
    }

    public static ActionGroup GeneratePickDropActionGroup()
    {
        const string orderId = "orderId";
        string agvId = string.Empty;

        var pickActionGuid = Guid.NewGuid();
        var dropActionGuid = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var pickAction = new AgvAction(pickActionGuid, agvId, ActionType.Pick, orderId, parentId, "Src-1")
        {
            Successor = dropActionGuid,
            Sequence = 0
        };
        var dropAction = new AgvAction(dropActionGuid, agvId, ActionType.Drop, orderId, parentId, "Dst-1")
        {
            Predecessor = pickActionGuid,
            Sequence = 1
        };
        return new ActionGroup(Guid.NewGuid(), new List<AgvAction>
        {
            pickAction,
            dropAction
        });
    }
}