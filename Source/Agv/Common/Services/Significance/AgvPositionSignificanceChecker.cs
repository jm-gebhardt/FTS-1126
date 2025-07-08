using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.Common.Services.Significance;

/// <summary>
/// Checks if <see cref="AgvPosition"/> has changed significantly.
/// </summary>
public class AgvPositionSignificanceChecker : ReflectionSignificanceChecker<AgvPosition>
{
    private readonly AgvPositionConfiguration _configuration;

    /// <inheritdoc cref="AgvPositionSignificanceChecker"/>
    /// <param name="configuration">The configuration for the significance.</param>
    public AgvPositionSignificanceChecker(AgvPositionConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    protected override Dictionary<string, Func<AgvPosition, AgvPosition, bool>> GetSignificantChangeByPropertyName() => new()
    {
        { nameof(AgvPosition.X), (obj1, obj2) => Math.Abs(obj1.X - obj2.X) >= _configuration.PositionThreshold },
        { nameof(AgvPosition.Y), (obj1, obj2) => Math.Abs(obj1.Y - obj2.Y) >= _configuration.PositionThreshold },
        { nameof(AgvPosition.Z), (obj1, obj2) => Math.Abs(obj1.Z - obj2.Z) >= _configuration.PositionThreshold },
        { nameof(AgvPosition.Theta), (obj1, obj2) => Math.Abs(obj1.Theta - obj2.Theta) >= _configuration.ThetaThreshold },
        { nameof(AgvPosition.DeviationRange), (obj1, obj2) => Math.Abs(obj1.DeviationRange.GetValueOrDefault() - obj2.DeviationRange.GetValueOrDefault()) >= _configuration.DeviationRangeThreshold }
    };
}