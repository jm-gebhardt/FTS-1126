using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Services;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.SignificanceChecker;

/// <summary>
/// Checks if <see cref="AgvPosition"/> has changed significantly.
/// </summary>
public class AgvPositionSignificanceChecker : ReflectionSignificanceChecker<AgvPosition>
{
    private const float PositionThreshold = 0.005f;
    private const float DeviationRangeThreshold = 0.001f;

    /// <inheritdoc />
    protected override Dictionary<string, Func<AgvPosition, AgvPosition, bool>> GetSignificantChangeByPropertyName() => new()
    {
        { nameof(AgvPosition.X), (obj1, obj2) => Math.Abs(obj1.X - obj2.X) >= PositionThreshold },
        { nameof(AgvPosition.Y), (obj1, obj2) => Math.Abs(obj1.Y - obj2.Y) >= PositionThreshold },
        { nameof(AgvPosition.Z), (obj1, obj2) => Math.Abs(obj1.Z - obj2.Z) >= PositionThreshold },
        { nameof(AgvPosition.Theta), (obj1, obj2) => Math.Abs(obj1.Theta - obj2.Theta) >= PositionThreshold },
        { nameof(AgvPosition.DeviationRange), (obj1, obj2) => Math.Abs(obj1.DeviationRange.GetValueOrDefault() - obj2.DeviationRange.GetValueOrDefault()) >= DeviationRangeThreshold }
    };
}