using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.OrderTranslator.Domain.Models.HostOrder;
using Gebhardt.Acc.OrderTranslator.Domain.Models.Station;
using JetBrains.Annotations;
using ActionGroup = Gebhardt.Acc.Common.Messages.ActionGroups.ActionGroup;

namespace Gebhardt.Acc.OrderTranslator.Test.Helper;

[PublicAPI]
public static class OrderGenerator
{
    public static readonly Station Ps01 = new("PS01",
        false,
        true,
        DoppelspielMode.Order,
        DoppelspielMode.Order,
        ["1020, 1040"],
        ["1050", "1060"],
        [new Routing("APL1", "map/NODE-9"), new Routing("AP01", "map/NODE-27")],
        [new Routing("APL1", "map/NODE-9"), new Routing("AP01", "map/NODE-32")],
        []);

    public static readonly Station Ps02 = new("PS02",
        false,
        true,
        DoppelspielMode.Order,
        DoppelspielMode.Order,
        ["1020, 1040"],
        ["1050", "1060"],
        [new Routing("APL1", "map/NODE-9"), new Routing("AP01", "map/NODE-27")],
        [new Routing("APL1", "map/NODE-9"), new Routing("AP01", "map/NODE-32")],
        []);

    public static readonly Station Apl1 = new("APL1",
        true,
        false,
        DoppelspielMode.Order,
        DoppelspielMode.OrderOrQueueHead,
        ["9060"],
        ["9070"],
        [],
        [],
        []);

    public static readonly Station Ap01 = new("AP01",
        false,
        false,
        DoppelspielMode.Always,
        DoppelspielMode.Order,
        ["2110"],
        ["2130"],
        [],
        [],
        ["AP02"]);

    public static readonly Station Ap02 = new("AP02",
        false,
        false,
        DoppelspielMode.Always,
        DoppelspielMode.Order,
        ["2110"],
        ["2130"],
        [],
        [],
        ["AP01"]);

    public static StationConfiguration TestConfiguration = new()
    {
        Stations = [Ps01, Apl1, Ap01, Ap02]
    };

    public static HostOrder GenerateOrderFromActionGroup(ActionGroup actionGroup)
    {
        var hostOrder = new HostOrder(actionGroup.OrderId, "hu", actionGroup.CreatedDate) { CorrelationId = actionGroup.CorrelationId };
        hostOrder.AddAction("Station", ActionType.Pick);
        hostOrder.AddAction("Station", ActionType.Drop);
        return hostOrder;
    }

    public static HostOrder GenerateOrder(Station sourceStation, Station targetStation)
    {
        var hostOrder = new HostOrder("orderId", "hu", DateTime.UtcNow) { CorrelationId = Guid.NewGuid() };
        hostOrder.AddAction(sourceStation.InputLanes.First(), ActionType.Pick);
        hostOrder.AddAction(targetStation.InputLanes.First(), ActionType.Drop);
        return hostOrder;
    }

    public static OrderDetails GenerateOrderDetails(Station sourceStation, Station targetStation, string hu)
    {
        return new OrderDetails(Guid.NewGuid(), "orderId", sourceStation.InputLanes.First(), targetStation.OutputLanes.First(), hu, HostOrderCreator.Wms) ;
    }

    public static ActionGroup GetActionGroupFromOrder(HostOrder order, Station pickStation, Station dropStation, bool isManual = false, bool withSap = false)
    {
        bool canRunDoppelspiel = dropStation.DropDoppelspielMode is not (DoppelspielMode.None or DoppelspielMode.OrderOrQueueHead);
        DateTime? consideredForDoppelspielDate = canRunDoppelspiel ? DateTime.UtcNow : null;
        return new ActionGroup(
                order.CorrelationId,
                order.TransportOrderId,
                [
                    order.Actions.Where(a => a.ActionType == ActionType.Pick)
                        .Select(a => new AgvAction(a.CorrelationId,
                            a.ActionType,
                            new ActionTarget(pickStation.Name.ToGraphNode(),
                                null,
                                [],
                                pickStation.PickDoppelspielMode),
                            order.CorrelationId,
                            a.InfoToAgv)
                        {
                            Sequence = 0,
                            AdditionalInfo = withSap ? $"SAP:{a.Node}" : null
                        })
                        .First(),
                    order.Actions.Where(a => a.ActionType == ActionType.Drop)
                        .Select(a => new AgvAction(a.CorrelationId,
                            a.ActionType,
                            new ActionTarget(dropStation.Name.ToGraphNode(),
                                null,
                                [],
                                dropStation.DropDoppelspielMode),
                            order.CorrelationId,
                            a.InfoToAgv)
                        {
                            Sequence = 1,
                            AdditionalInfo = withSap ? $"SAP:{a.Node}" : null
                        })
                        .First()
                ],
                ActionGroupCreator.Wms,
                Priority.Default,
                false,
                order.CreatedDate)
            { HandlingUnit = order.Hu, CanRunDoppelspiel = canRunDoppelspiel, ConsideredForDoppelspielDate = consideredForDoppelspielDate, IsManual = isManual};
    }
}