using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.Common.Services.Significance;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Filters.SignificanceCheckers;

/// <summary>
/// Checks if <see cref="AgvPositionDto"/> has changed significantly.
/// </summary>
public class AgvPositionSignificanceChecker : ReflectionSignificanceChecker<AgvPositionDto>
{
    //private const double PositionThreshold = 0.005;
    //private const double DeviationRangeThreshold = 0.001;
    private readonly AgvPositionConfiguration _agvPositionConfiguration;

    /// <inheritdoc />
    public AgvPositionSignificanceChecker(AgvPositionConfiguration agvPositionConfiguration)
    {
        _agvPositionConfiguration = agvPositionConfiguration;
    }
    
    /// <inheritdoc />
    protected override Dictionary<string, Func<AgvPositionDto, AgvPositionDto, bool>> GetSignificantChangeByPropertyName() => new()
    {
        { nameof(AgvPositionDto.X), (obj1, obj2) => Math.Abs(obj1.X - obj2.X) >= _agvPositionConfiguration.PositionThreshold },
        { nameof(AgvPositionDto.Y), (obj1, obj2) => Math.Abs(obj1.Y - obj2.Y) >= _agvPositionConfiguration.PositionThreshold },
        { nameof(AgvPositionDto.Z), (obj1, obj2) => Math.Abs(obj1.Z - obj2.Z) >= _agvPositionConfiguration.PositionThreshold },
        { nameof(AgvPositionDto.Theta), (obj1, obj2) => Math.Abs(obj1.Theta - obj2.Theta) >= _agvPositionConfiguration.ThetaThreshold },
        { nameof(AgvPositionDto.DeviationRange), (obj1, obj2) => Math.Abs(obj1.DeviationRange.GetValueOrDefault() - obj2.DeviationRange.GetValueOrDefault()) >= _agvPositionConfiguration.DeviationRangeThreshold }
    };
}