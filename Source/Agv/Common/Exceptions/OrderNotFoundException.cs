using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// Order not found
/// </summary>
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