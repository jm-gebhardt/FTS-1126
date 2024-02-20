using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Services;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.SignificanceChecker;

/// <summary>
/// Checks if <see cref="AgvPosition"/> has changed significantly.
/// If any of the objects is null, it will be considered as a significant change.
/// </summary>
public class AgvPositionNullableSignificanceChecker : ISignificanceChecker<AgvPosition?>
{
    private readonly ISignificanceChecker<AgvPosition> _significanceChecker;

    /// <inheritdoc cref="AgvPositionNullableSignificanceChecker"/>
    public AgvPositionNullableSignificanceChecker(ISignificanceChecker<AgvPosition> significanceChecker)
    {
        _significanceChecker = significanceChecker;
    }

    /// <inheritdoc />
    public bool HasSignificantChanges(AgvPosition? obj1, AgvPosition? obj2)
    {
        if (obj1 == null && obj2 == null)
        {
            return false;
        }
        if (obj1 == null || obj2 == null)
        {
            return true;
        }

        return _significanceChecker.HasSignificantChanges(obj1, obj2);
    }
}