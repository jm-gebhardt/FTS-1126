using Gebhardt.Acc.Common.Messages.AgvManagement;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Exceptions;

/// <summary>
/// Order not found
/// </summary>
[UsedImplicitly]
public class OrderNotFoundException : NotFoundException
{
    /// <summary>
    /// Order with order correlationId was not found
    /// </summary>
    /// <param name="correlationId">Correlation Id of the order</param>
    public OrderNotFoundException(Guid correlationId)
        : base(nameof(IOrder), nameof(correlationId), correlationId)
    {
    }
}