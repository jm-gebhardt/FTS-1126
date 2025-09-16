using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;

namespace Gebhardt.Acc.Common.Testing.Helpers;

public static class AgvStatusGenerator
{
    public static AgvStatus Generate(
        Guid? agvCorrelationId = null,
        string orderId = "order",
        bool busy = false,
        double batteryCharge = 40,
        bool charging = false,
        string? lastNodeId = null,
        ICollection<string>? zoneIds = null,
        ICollection<string>? stationIds = null
    )
    {
        return new AgvStatus(agvCorrelationId ?? Guid.NewGuid(),
            orderId,
            busy,
            true,
            batteryCharge,
            charging,
            48,
            charging ? 20 : -10,
            20,
            false,
            lastNodeId?.ToGuid(),
            null,
            null,
            zoneIds, stationIds);
    }
}