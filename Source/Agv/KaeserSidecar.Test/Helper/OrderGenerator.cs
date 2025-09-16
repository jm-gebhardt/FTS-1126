using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using JetBrains.Annotations;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.Acc.KaeserSidecar.Test.Helper;

[PublicAPI]
public static class OrderGenerator
{
    public static HostOrder GenerateOrderFromActionGroup(ActionGroup actionGroup)
    {
        var hostOrder = new HostOrder(actionGroup.OrderId, "hu", actionGroup.CreatedDate) { CorrelationId = actionGroup.CorrelationId };
        hostOrder.AddAction("KaeserStation", ActionType.Pick);
        hostOrder.AddAction("KaeserStation", ActionType.Drop);
        return hostOrder;
    }

    public static TransportOrder.Domain.Models.ActionGroup.ActionGroup GenerateActionGroupFromOrder(HostOrder order, ActionType? startedActionType)
    {
        List<AgvAction> actions = order.Actions.Select(a => new AgvAction(a.CorrelationId, a.ActionType, a.Node.ToGraphNode(), order.CorrelationId)).ToList();

        var actionGroup = new TransportOrder.Domain.Models.ActionGroup.ActionGroup(order.CorrelationId, order.Hu, actions, ActionGroupCreator.Wms, assignedAgv: order.AssignedAgv);
        if (startedActionType != null)
        {
            var action = actions.Single(a => a.ActionType == startedActionType);
            actionGroup.StartAction(action.CorrelationId, [], []);
        }
        return actionGroup;
    }
}