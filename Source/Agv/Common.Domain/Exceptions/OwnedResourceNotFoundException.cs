using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Exceptions;

/// <inheritdoc />
[PublicAPI]
public class OwnedResourceNotFoundException : NotFoundException
{
    /// <inheritdoc />
    public OwnedResourceNotFoundException(string paramName) : base(paramName)
    {
    }

    /// <inheritdoc />
    public OwnedResourceNotFoundException(string paramName, string paramIdentifierName, object? paramIdentifierValue) : base(paramName, paramIdentifierName, paramIdentifierValue)
    {
    }
}