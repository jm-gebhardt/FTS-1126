using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// Represents the exception thrown when a property was not found.
/// </summary>
public class PropertyNotFoundException : NotFoundException
{
    /// <inheritdoc cref="PropertyNotFoundException"/>>
    public PropertyNotFoundException(string paramName) : base(paramName)
    {
    }

    /// <inheritdoc cref="PropertyNotFoundException"/>>
    [PublicAPI]
    public PropertyNotFoundException(string paramName, string paramIdentifierName, object? paramIdentifierValue) : base(paramName, paramIdentifierName, paramIdentifierValue)
    {
    }
}