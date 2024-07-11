namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represents the exception thrown, when a trigger assignment failed.
/// </summary>
public class TriggerAssignmentFailedException: Exception
{
    /// <inheritdoc cref="TriggerAssignmentFailedException"/>>
    public TriggerAssignmentFailedException(string message) : base(message)
    {
    }
}