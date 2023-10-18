
namespace Gebhardt.StoreWare.Agv.WCSCommunication.Domain.Exceptions;

/// <summary>
/// Represents the exception thrown when an order could not be found.
/// </summary>
public class OrderNotFoundException : Exception
{
    /// <inheritdoc cref="OrderNotFoundException"/>
    /// <param name="orderId">The id of the order.</param>
    public OrderNotFoundException(string orderId) : base($" Order {orderId} not found")
    {
    }
}