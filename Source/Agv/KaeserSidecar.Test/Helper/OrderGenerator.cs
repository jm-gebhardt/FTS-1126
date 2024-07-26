using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;

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
}