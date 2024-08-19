using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represents the exception raised when a restricted area has not been found
/// </summary>
public class RestrictedAreaNotFoundException: NotFoundException
{
    /// <inheritdoc cref="RestrictedAreaNotFoundException"/>>
    public RestrictedAreaNotFoundException(Guid id) : base($"Restricted area {id}")
    {
    }

    /// <inheritdoc cref="RestrictedAreaNotFoundException"/>>
    public RestrictedAreaNotFoundException(ICollection<Guid> ids)
    : base($"Unknown restricted area ids: {string.Join(',', ids.Select(c => c.ToString()))}")
    {
    }
}