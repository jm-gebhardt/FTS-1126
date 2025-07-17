using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Services.Filters;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Filters;

/// <summary>
/// Filter by changes in any messages implementing <see cref="IMessageCreatedDate"/>
/// and <see cref="CorrelatedBy{Guid}"/>
/// </summary>
public class PushModeSignificanceChecker<T>: ISignificanceChecker<T> where T: class, IMessageCreatedDate, CorrelatedBy<Guid>
{
    /// <inheritdoc cref="ISignificanceChecker{T}"/>>
    public bool HasSignificantChanges(T obj1, T obj2)
    {
        if (obj1.MessageCreatedDate - obj2.MessageCreatedDate >= TimeSpan.FromMinutes(30) || obj2.CorrelationId != obj1.CorrelationId)
        {
            return true;
        }
        return false;
    }
}