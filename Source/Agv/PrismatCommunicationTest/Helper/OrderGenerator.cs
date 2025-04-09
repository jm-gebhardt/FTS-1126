using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.PrismatCommunicationTest.Helper;

/// <summary>
/// Provides orders for test cases.
/// </summary>
public static class OrderGenerator
{
    public const string DecisionNode = "Waypoint1";

    [PublicAPI]
    public const string CheckCargoTriggerNode = "Waypoint2";

    [PublicAPI]
    public const string ShortLoopTriggerNode = "Waypoint3";

    private static KaeserGraphConfiguration Configuration { get; } = new(new InMemoryTriggerPointMapping());

    public static KaeserGraphConfiguration KaeserConfiguration
    {
        get
        {
            Configuration.UpdateTriggerPointNodeId(Configuration.DecisionPoint1TriggerName, DecisionNode);
            Configuration.UpdateLongLoopMoveTargetNode(DecisionNode);
            Configuration.UpdateTriggerPointNodeId(Configuration.CheckCargoTriggerName, CheckCargoTriggerNode);
            Configuration.UpdateTriggerPointNodeId(Configuration.DoppelspielTriggerName, ShortLoopTriggerNode);
            return Configuration;
        }
    }

    public static OrderDetails GenerateShortCircuitOrder(bool pickAt1020 = true, int huRotation = 1)
    {
        if (pickAt1020)
        {
            return new OrderDetails(Guid.NewGuid(),
                "OrderId",
                "1020",
                KaeserConfiguration.PrismatKnownTargets.First(t => t.StartsWith('2')),
                Guid.NewGuid().ToString(),
                HostOrderCreator.Wms,
                huRotation);
        }
        return new OrderDetails(Guid.NewGuid(),
            "OrderId",
            KaeserConfiguration.PrismatKnownTargets.First(t => t.StartsWith('2')),
            "1050",
            Guid.NewGuid().ToString(),
            HostOrderCreator.Wms, huRotation);
    }

    public static OrderDetails GenerateUnknownPickTargetOrder(int pickTarget = 0)
    {
        return new OrderDetails(Guid.NewGuid(),
            "OrderId",
            KaeserConfiguration.PrismatInputLanes[pickTarget],
            KaeserConfiguration.PrismatAplTarget,
            Guid.NewGuid().ToString(),
            HostOrderCreator.Wms);
    }

    public static OrderDetails GenerateUnknownDropTargetOrder(string? hu=null, int dropTarget = 0)
    {
        return new OrderDetails(Guid.NewGuid(),
            "OrderId",
            KaeserConfiguration.PrismatAplSource,
            KaeserConfiguration.PrismatOutputLanes[dropTarget],
            hu??Guid.NewGuid().ToString(),
            HostOrderCreator.Wms);
    }

    public static HostOrder GenerateHostOrderFromOrderDetails(OrderDetails orderDetails, DateTime? createdDate=null, DateTime? finishDate = null)
    {
        var hostOrder = new HostOrder(orderDetails.OrderId, orderDetails.LeNo, createdDate??DateTime.UtcNow);
        hostOrder.AddAction(orderDetails.Source, ActionType.Pick);
        hostOrder.AddAction(orderDetails.Destination, ActionType.Drop);
        if (finishDate != null)
        {
            hostOrder.Finish(finishDate.Value);
        }

        return hostOrder;
    }
}