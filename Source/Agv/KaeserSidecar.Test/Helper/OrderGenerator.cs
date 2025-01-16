using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.Acc.KaeserSidecar.Test.Helper;

public static class OrderGenerator
{
    public static HostOrder GenerateOrderFromActionGroup(ActionGroup actionGroup)
    {
        var hostOrder = new HostOrder(actionGroup.CorrelationId, actionGroup.OrderId, "hu", actionGroup.CreatedDate);
        hostOrder.AddAction("KaeserStation", ActionType.Pick);
        hostOrder.AddAction("KaeserStation", ActionType.Drop);
        return hostOrder;
    }

    public static TransportOrder.Domain.Models.ActionGroup.ActionGroup GenerateActionGroupFromOrder(HostOrder order,  ActionType? startedActionType)
    {
        List<AgvAction> actions = order.Actions.Select(a => new AgvAction(a.CorrelationId, a.ActionType,a.Node, order.CorrelationId)).ToList();
        
        var actionGroup = new TransportOrder.Domain.Models.ActionGroup.ActionGroup(order.CorrelationId, order.OrderId, actions, ActionGroupCreator.Wms, assignedAgv: order.AssignedAgv);
        if (startedActionType != null)
        {
            var action = actions.Single(a => a.ActionType == startedActionType);
            actionGroup.StartAction(action.CorrelationId, [], []);
        }
        return actionGroup;
    }
}