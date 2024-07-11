using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Trigger not found
/// </summary>
public class TriggerNotFoundException : NotFoundException
{
    /// <inheritdoc cref="TriggerNotFoundException"/>>
    public TriggerNotFoundException(Guid id) : base($"Trigger {id}")
    {
    }
}