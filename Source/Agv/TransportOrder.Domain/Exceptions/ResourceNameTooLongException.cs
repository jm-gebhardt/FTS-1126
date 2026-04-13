using JetBrains.Annotations;
using Gebhardt.Acc.Common.Persistence.Ef;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Exception raised when a resources name is longer than the database string limitation.
/// </summary>
[PublicAPI]
public class ResourceNameTooLongException : Exception
{
    /// <inheritdoc/>
    public ResourceNameTooLongException(string name, int threshold) : base($"{name} too long, needs to be <= {threshold} characters"){}
    
    /// <inheritdoc/>
    public ResourceNameTooLongException(string name) : base($"{name} to long, needs to be <= {DbStringLimits.Description} characters"){}
}