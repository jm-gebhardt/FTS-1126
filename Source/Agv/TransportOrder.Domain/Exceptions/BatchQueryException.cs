namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represents errors that occur when the object is not in correct state
/// </summary>
public class BatchQueryException : Exception
{
    /// <inheritdoc cref="BatchQueryException"/>
    /// <param name="message">Exception message</param>
    protected BatchQueryException(string message) : base(message)
    {
    }
}