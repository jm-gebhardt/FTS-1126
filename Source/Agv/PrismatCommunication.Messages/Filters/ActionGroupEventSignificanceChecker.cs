using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Services.Filters;
using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Messages.Filters;

/// <summary>
/// Filter by changes in any messages implementing <see cref="IActionEventParent"/>
/// </summary>
/// <typeparam name="T"></typeparam>
public class ActionGroupEventSignificanceChecker<T> : ISignificanceChecker<T> where T : class, CorrelatedBy<Guid>
{
    /// <inheritdoc cref="ISignificanceChecker{T}"/>>
    public bool HasSignificantChanges(T obj1, T obj2)
    {
        if (!obj1.CorrelationId.Equals(obj2.CorrelationId))
        {
            return true;
        }
        return false;
    }
}