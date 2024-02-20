using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Filters.SignificanceCheckers;

/// <inheritdoc />
public class AgvPositionSignificanceChecker : ReflectionSignificanceChecker<AgvPositionDto>
{
    private const double PositionThreshold = 0.005;
    private const double DeviationRangeThreshold = 0.001;

    /// <inheritdoc />
    protected override Dictionary<string, Func<AgvPositionDto, AgvPositionDto, bool>> GetSignificantChangeByPropertyName() => new()
    {
        { nameof(AgvPositionDto.X), (obj1, obj2) => Math.Abs(obj1.X - obj2.X) >= PositionThreshold },
        { nameof(AgvPositionDto.Y), (obj1, obj2) => Math.Abs(obj1.Y - obj2.Y) >= PositionThreshold },
        { nameof(AgvPositionDto.Z), (obj1, obj2) => Math.Abs(obj1.Z - obj2.Z) >= PositionThreshold },
        { nameof(AgvPositionDto.Theta), (obj1, obj2) => Math.Abs(obj1.Theta - obj2.Theta) >= PositionThreshold },
        { nameof(AgvPositionDto.DeviationRange), (obj1, obj2) => Math.Abs(obj1.DeviationRange.GetValueOrDefault() - obj2.DeviationRange.GetValueOrDefault()) >= DeviationRangeThreshold }
    };
}