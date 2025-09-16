using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

public static class AgvStatusGenerator
{
    public static AgvStatus Generate(Guid? agvCorrelationId = null,
        string orderId = "order",
        bool busy = false,
        double batteryCharge = 40,
        bool charging = false,
        string? lastNodeName = null,
        ICollection<string>? zoneIds = null,
        ICollection<string>? stationIds = null)
    {
        return new AgvStatus(agvCorrelationId ?? Guid.NewGuid(),
            orderId,
            busy,
            true,
            batteryCharge,
            charging,
            false,
            lastNodeName?.ToGuid(),
            48,
            charging ? 20 : -10,
            20,
            null,
            null,
            zoneIds, stationIds);
    }
}