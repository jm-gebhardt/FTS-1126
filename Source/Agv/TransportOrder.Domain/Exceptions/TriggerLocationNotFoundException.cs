using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Trigger location not found
/// </summary>
public class TriggerLocationNotFoundException:NotFoundException
{
    /// <inheritdoc cref="TriggerLocationNotFoundException"/>>
    public TriggerLocationNotFoundException(Guid id) : base($"Trigger location {id}")
    {
    }
}