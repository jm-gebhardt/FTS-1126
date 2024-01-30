using Gebhardt.StoreWare.Agv.WCSCommunication.Domain.Models.HostOrder;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Domain.Exceptions;

/// <summary>
/// Represents the exception thrown when a <see cref="HostOrder"/> could not be found.
/// </summary>
public class HostOrderNotFoundException : Exception
{
    /// <inheritdoc cref="HostOrderNotFoundException"/>
    /// <param name="correlationId">The correlation id of the order.</param>
    public HostOrderNotFoundException(Guid correlationId) : base($" Host order {correlationId} not found")
    {
    }
}