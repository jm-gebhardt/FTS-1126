using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;

namespace Gebhardt.Acc.Common.Testing.Helpers;

public static class AgvStatusGenerator
{
    public static AgvStatus Generate(
        Guid? agvCorrelationId = null,
        string? orderId = "order",
        bool busy = false,
        bool automatic = true,
        double batteryCharge = 40,
        bool charging = false,
        bool disturbed = false,
        string? lastNodeName = null,
        double batteryVoltage = 48,
        double? batteryCurrent = null,
        double batteryTemperature = 20,
        DateTime? estimatedFullyChargedTime = null,
        DateTime? lastBatteryReplacement = null,
        ICollection<string>? zoneIds = null,
        ICollection<string>? stationIds = null,
        float totalDistance = 0,
        float totalLifetime = 0,
        AgvOperatingMode operatingMode = AgvOperatingMode.Automatic
    )
    {
        return new AgvStatus(agvCorrelationId ?? Guid.NewGuid(),
            orderId,
            busy,
            automatic,
            batteryCharge,
            charging,
            batteryVoltage,
            batteryCurrent ?? (charging ? 20 : -10),
            batteryTemperature,
            disturbed,
            lastNodeName?.ToGuid(),
            estimatedFullyChargedTime,
            lastBatteryReplacement,
            zoneIds,
            stationIds,
            totalDistance,
            totalLifetime,
            operatingMode);
    }

    public static AgvStatus Generate(
        Guid agvCorrelationId,
        string? orderId,
        bool busy,
        bool automatic,
        double batteryCharge,
        bool charging,
        bool disturbed,
        Guid? lastNodeIdGuid = null,
        double batteryVoltage = 48,
        double? batteryCurrent = null,
        double batteryTemperature = 20,
        DateTime? estimatedFullyChargedTime = null,
        DateTime? lastBatteryReplacement = null,
        ICollection<string>? zoneIds = null,
        ICollection<string>? stationIds = null,
        float totalDistance = 0,
        float totalLifetime = 0,
        AgvOperatingMode operatingMode = AgvOperatingMode.Automatic
    )
    {
        return new AgvStatus(agvCorrelationId,
            orderId,
            busy,
            automatic,
            batteryCharge,
            charging,
            batteryVoltage,
            batteryCurrent ?? (charging ? 20 : -10),
            batteryTemperature,
            disturbed,
            lastNodeIdGuid,
            estimatedFullyChargedTime,
            lastBatteryReplacement,
            zoneIds,
            stationIds,
            totalDistance,
            totalLifetime,
            operatingMode);
    }
}