namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Exception thrown when attempting to restore a trigger that is not marked as deleted.
/// </summary>
public class TriggerNotDeletedException : Exception
{
    /// <inheritdoc cref="TriggerNotDeletedException"/>
    /// <param name="triggerId">The trigger id.</param>
    public TriggerNotDeletedException(Guid triggerId)
        : base($"Trigger {triggerId} is not marked as deleted and cannot be restored.")
    {
    }
}

