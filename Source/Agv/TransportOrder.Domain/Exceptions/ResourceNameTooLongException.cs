using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Exception raised when a resources name is longer than the database string limitation.
/// </summary>
public class ResourceNameTooLongException : Exception
{
    /// <inheritdoc/>
    [PublicAPI]
    public ResourceNameTooLongException(string name, int threshold) : base($"{name} too long, needs to be <= {threshold} characters")
    {
    }
}