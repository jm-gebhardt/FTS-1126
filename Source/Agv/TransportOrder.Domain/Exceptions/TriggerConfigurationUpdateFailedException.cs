namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represents the exception raised when updating the trigger configuration failed.
/// </summary>
public class TriggerConfigurationUpdateFailedException: Exception
{
    /// <inheritdoc cref="TriggerConfigurationUpdateFailedException"/>>
    public TriggerConfigurationUpdateFailedException(string message) : base(message)
    {
    }
}