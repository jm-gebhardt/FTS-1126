using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Services.Significance;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Filters.SignificanceCheckers;

/// <inheritdoc />
public class AgvBatteryStatusSignificanceChecker : ReflectionSignificanceChecker<AgvBatteryStatusDto>
{
    private readonly AgvBatteryConfiguration _agvBatteryConfiguration;

    /// <inheritdoc />
    public AgvBatteryStatusSignificanceChecker(AgvBatteryConfiguration agvBatteryConfiguration)
    {
        _agvBatteryConfiguration = agvBatteryConfiguration;
    }

    /// <inheritdoc />
    protected override Dictionary<string, Func<AgvBatteryStatusDto, AgvBatteryStatusDto, bool>> GetSignificantChangeByPropertyName() => new()
    {
        { nameof(AgvBatteryStatusDto.BatteryCharge), (obj1, obj2) => Math.Abs(obj1.BatteryCharge - obj2.BatteryCharge) >= _agvBatteryConfiguration.BatteryChargeThreshold },
        { nameof(AgvBatteryStatusDto.BatteryVoltage), (obj1, obj2) => Math.Abs(obj1.BatteryVoltage - obj2.BatteryVoltage) >= _agvBatteryConfiguration.BatteryVoltageThreshold },
        { nameof(AgvBatteryStatusDto.BatteryCurrent), (obj1, obj2) => Math.Abs(obj1.BatteryCurrent - obj2.BatteryCurrent) >= _agvBatteryConfiguration.BatteryCurrentThreshold },
        { nameof(AgvBatteryStatusDto.BatteryTemperature), (obj1, obj2) => Math.Abs(obj1.BatteryTemperature - obj2.BatteryTemperature) >= _agvBatteryConfiguration.BatteryTemperatureThreshold },
        // just exclude EstimatedFullyChargedTime, because it changes with every init of AgvStatus, as this is not fully implemented yet.
        { nameof(AgvBatteryStatusDto.EstimatedFullyChargedTime), (_, _) => false },
        { nameof(AgvBatteryStatusDto.LastBatteryReplacement), (obj1, obj2) => obj1.LastBatteryReplacement != obj2.LastBatteryReplacement },
        // message created date will be considered in the message filter
        { nameof(AgvBatteryStatusDto.MessageCreatedDate), (_, _) => false }
    };
}