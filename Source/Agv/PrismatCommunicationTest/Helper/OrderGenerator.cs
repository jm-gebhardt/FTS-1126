using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.CustomerConfiguration;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using JetBrains.Annotations;

namespace Gebhardt.Acc.PrismatCommunicationTest.Helper;

/// <summary>
/// Provides orders for test cases.
/// </summary>
public static class OrderGenerator
{
    [PublicAPI]
    public const string DecisionNode = "Waypoint1";

    [PublicAPI]
    public const string CheckCargoTriggerNode = "Waypoint2";

    [PublicAPI]
    public const string ShortLoopTriggerNode = "Waypoint3";

    private static CustomerConfiguration Configuration { get; } = new();

    public static CustomerConfiguration CustomerConfiguration => Configuration;

    public static OrderDetails GenerateShortCircuitOrder(bool pickAt1020 = true, int huRotation = 1)
    {
        if (pickAt1020)
        {
            return new OrderDetails(Guid.NewGuid(),
                "OrderId",
                "1020",
                "2110",
                Guid.NewGuid().ToString(),
                HostOrderCreator.Wms,
                huRotation);
        }
        return new OrderDetails(Guid.NewGuid(),
            "OrderId",
             "2130",
            "1050",
            Guid.NewGuid().ToString(),
            HostOrderCreator.Wms, huRotation);
    }

    public static OrderDetails GenerateOrderDetails(string source, string target)
    {
        return new OrderDetails(Guid.NewGuid(),
            "OrderId",
            source,
            target,
            Guid.NewGuid().ToString(),
            HostOrderCreator.Wms);
    }

    public static OrderDetails GenerateUnknownPickTargetOrder()
    {
        return new OrderDetails(Guid.NewGuid(),
            "OrderId",
            "1020",
            "9070",
            Guid.NewGuid().ToString(),
            HostOrderCreator.Wms);
    }

    public static OrderDetails GenerateUnknownDropTargetOrder(string? hu = null)
    {
        return new OrderDetails(Guid.NewGuid(),
            "OrderId",
            "9060",
            "1050",
            hu ?? Guid.NewGuid().ToString(),
            HostOrderCreator.Wms);
    }

    public static HostOrder GenerateHostOrderFromOrderDetails(OrderDetails orderDetails, DateTime? createdDate = null, DateTime? finishDate = null)
    {
        var hostOrder = new HostOrder(orderDetails.OrderId, orderDetails.LeNo, createdDate ?? DateTime.UtcNow);
        hostOrder.AddAction(orderDetails.Source, ActionType.Pick);
        hostOrder.AddAction(orderDetails.Destination, ActionType.Drop);
        if (finishDate != null)
        {
            hostOrder.Finish(finishDate.Value);
        }

        return hostOrder;
    }
}