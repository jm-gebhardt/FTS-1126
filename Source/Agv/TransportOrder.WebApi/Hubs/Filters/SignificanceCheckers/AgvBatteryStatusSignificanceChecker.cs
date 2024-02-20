using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Filters.SignificanceCheckers;

/// <inheritdoc />
public class AgvBatteryStatusSignificanceChecker : ReflectionSignificanceChecker<AgvBatteryStatusDto>
{
    private const double BatteryChargeThreshold = 0.3;

    /// <inheritdoc />
    protected override Dictionary<string, Func<AgvBatteryStatusDto, AgvBatteryStatusDto, bool>> GetSignificantChangeByPropertyName() => new()
    {
        { nameof(AgvBatteryStatusDto.BatteryCharge), (obj1, obj2) => Math.Abs(obj1.BatteryCharge - obj2.BatteryCharge) >= BatteryChargeThreshold },
        { nameof(AgvPositionStatusDto.MessageCreatedDate), (_, _) => false }
    };
}