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
            Configuration.UpdateTriggerPointNodeId(Configuration.CheckCargoTriggerName, CheckCargoTriggerNode);
            Configuration.UpdateTriggerPointNodeId(Configuration.ShortLoopTriggerName, ShortLoopTriggerNode);
            return Configuration;
        }
    }

    public static OrderDetails GenerateShortCircuitOrder(bool pickAt1020 = true, string huRotation = "")
    {
        if (pickAt1020)
        {
            return new OrderDetails(Guid.NewGuid(),
                "OrderId",
                "1020",
                KaeserConfiguration.PrismatKnownTargets.First(t => t.StartsWith('2')),
                "leNo",
                HostOrderCreator.Wms,
                huRotation);
        }
        return new OrderDetails(Guid.NewGuid(),
            "OrderId",
            KaeserConfiguration.PrismatKnownTargets.First(t => t.StartsWith('2')),
            "1050",
            "leNo",
            HostOrderCreator.Wms, huRotation);
    }

    public static OrderDetails GenerateUnknownPickTargetOrder()
    {
        return new OrderDetails(Guid.NewGuid(),
            "OrderId",
            KaeserConfiguration.PrismatIncompletePickTargets.First(),
            KaeserConfiguration.PrismatIncompletePickTargets.Last(),
            "leNo",
            HostOrderCreator.Wms);
    }

    public static OrderDetails GenerateUnknownDropTargetOrder()
    {
        return new OrderDetails(Guid.NewGuid(),
            "OrderId",
            KaeserConfiguration.PrismatIncompleteDropTargets.First(),
            KaeserConfiguration.PrismatIncompleteDropTargets.Last(),
            "leNo",
            HostOrderCreator.Wms);
    }

    public static HostOrder GenerateHostOrderFromOrderDetails(OrderDetails orderDetails)
    {
        var hostOrder = new HostOrder(orderDetails.CorrelationId, orderDetails.OrderId, "handlingUnit", DateTime.UtcNow);
        hostOrder.AddAction(orderDetails.Source, ActionType.Pick);
        hostOrder.AddAction(orderDetails.Destination, ActionType.Drop);

        return hostOrder;
    }
}