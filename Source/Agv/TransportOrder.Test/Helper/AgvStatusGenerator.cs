using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

public static class AgvStatusGenerator
{
    public static AgvStatus Generate(
        Guid? agvCorrelationId = null,
        string? orderId = null,
        bool busy = false,
        bool automatic = true,
        double batteryCharge = 40,
        bool charging = false,
        bool disturbed = false,
        Guid? lastNodeIdGuid = null,
        string? lastNodeName = null,
        double batteryVoltage = 48,
        double batteryCurrent = 0,
        double batteryTemperature = 20,
        DateTime? estimatedFullyChargedTime = null,
        DateTime? lastBatteryReplacement = null,
        ICollection<string>? zoneIds = null,
        ICollection<string>? stationIds = null,
        float totalDistance = 0,
        float totalLifetime = 0)
    {
        return new AgvStatus(agvCorrelationId ?? Guid.NewGuid(),
            orderId,
            busy,
            automatic,
            batteryCharge,
            charging,
            disturbed,
            lastNodeIdGuid ?? lastNodeName?.ToGuid(),
            batteryVoltage,
            charging ? batteryCurrent : -batteryCurrent,
            batteryTemperature,
            estimatedFullyChargedTime,
            lastBatteryReplacement,
            zoneIds,
            stationIds,
            totalDistance,
            totalLifetime
        );
    }
}