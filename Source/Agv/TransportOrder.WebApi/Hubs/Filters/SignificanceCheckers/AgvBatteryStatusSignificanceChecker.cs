using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Filters.SignificanceCheckers;

/// <inheritdoc />
public class AgvBatteryStatusSignificanceChecker : ReflectionSignificanceChecker<AgvBatteryStatusDto>
{
    private const double BatteryChargeThreshold = 0.3;
    private const double BatteryVoltageThreshold = 0.2;
    private const double BatteryCurrentThreshold = 0.5;
    private const double BatteryTemperatureThreshold = 0.5;

    /// <inheritdoc />
    protected override Dictionary<string, Func<AgvBatteryStatusDto, AgvBatteryStatusDto, bool>> GetSignificantChangeByPropertyName() => new()
    {
        { nameof(AgvBatteryStatusDto.BatteryCharge), (obj1, obj2) => Math.Abs(obj1.BatteryCharge - obj2.BatteryCharge) >= BatteryChargeThreshold },
        { nameof(AgvBatteryStatusDto.BatteryVoltage), (obj1, obj2) => Math.Abs(obj1.BatteryVoltage - obj2.BatteryVoltage) >= BatteryVoltageThreshold },
        { nameof(AgvBatteryStatusDto.BatteryCurrent), (obj1, obj2) => Math.Abs(obj1.BatteryCurrent - obj2.BatteryCurrent) >= BatteryCurrentThreshold },
        { nameof(AgvBatteryStatusDto.BatteryTemperature), (obj1, obj2) => Math.Abs(obj1.BatteryTemperature - obj2.BatteryTemperature) >= BatteryTemperatureThreshold },
        // just exclude EstimatedFullyChargedTime and LastBatteryReplacement, because they change with every init of AgvStatus, as this is not fully implemented yet.
        { nameof(AgvBatteryStatusDto.EstimatedFullyChargedTime), (_, _) => false },
        { nameof(AgvBatteryStatusDto.LastBatteryReplacement), (_, _) => false },
        // message created date will be considered in the hub filter
        { nameof(AgvBatteryStatusDto.MessageCreatedDate), (_, _) => false }
    };
}