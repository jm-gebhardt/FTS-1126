using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// The exception raised, when a background could not be found.
/// </summary>
public class BackgroundNotFoundException : NotFoundException
{
    /// <inheritdoc/>   
    public BackgroundNotFoundException(Guid id) : base($"Background {id}") { }
}