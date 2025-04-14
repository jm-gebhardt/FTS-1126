using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Services.Filters;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Filters;

/// <summary>
/// Filter by changes in any messages implementing <see cref="IAgvActionEvent"/>
/// </summary>
/// <typeparam name="T"></typeparam>
public class AgvActionSignificanceChecker<T> : ISignificanceChecker<T> where T: class, IAgvActionEvent
{
    /// <inheritdoc cref="ISignificanceChecker{T}"/>>
    public bool HasSignificantChanges(T obj1, T obj2)
    {
        if (!obj1.AgvCorrelationId.Equals(obj2.AgvCorrelationId))
        {
            return true;
        }

        if (!obj1.CorrelationId.Equals(obj2.CorrelationId))
        {
            return true;
        }
        return false;
    }
}