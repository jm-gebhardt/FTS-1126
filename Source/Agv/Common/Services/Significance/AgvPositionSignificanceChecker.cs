using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.Common.Services.Significance;

/// <summary>
/// Checks if <see cref="AgvPosition"/> has changed significantly.
/// </summary>
public class AgvPositionSignificanceChecker : ReflectionSignificanceChecker<AgvPosition>
{
    private readonly AgvPositionConfiguration _agvPositionConfiguration;

    /// <inheritdoc cref="AgvPositionSignificanceChecker"/>
    /// <param name="agvPositionConfiguration">The configuration for the significance.</param>
    public AgvPositionSignificanceChecker(AgvPositionConfiguration agvPositionConfiguration)
    {
        _agvPositionConfiguration = agvPositionConfiguration;
    }

    /// <inheritdoc />
    protected override Dictionary<string, Func<AgvPosition, AgvPosition, bool>> GetSignificantChangeByPropertyName() => new()
    {
        {nameof(AgvPosition.X), (obj1, obj2) => Math.Abs(obj1.X - obj2.X) >= _agvPositionConfiguration.PositionThreshold},
        {nameof(AgvPosition.Y), (obj1, obj2) => Math.Abs(obj1.Y - obj2.Y) >= _agvPositionConfiguration.PositionThreshold},
        {nameof(AgvPosition.Z), (obj1, obj2) => Math.Abs(obj1.Z - obj2.Z) >= _agvPositionConfiguration.PositionThreshold},
        {nameof(AgvPosition.Theta), (obj1, obj2) => Math.Abs(obj1.Theta - obj2.Theta) >= _agvPositionConfiguration.ThetaThreshold},
        {nameof(AgvPosition.DeviationRange), (obj1, obj2) => Math.Abs(obj1.DeviationRange.GetValueOrDefault() - obj2.DeviationRange.GetValueOrDefault()) >= _agvPositionConfiguration.DeviationRangeThreshold}
    };
}