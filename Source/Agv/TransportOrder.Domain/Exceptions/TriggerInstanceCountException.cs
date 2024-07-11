namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represents the exception thrown when the trigger instance count exceeds the maximum.
/// </summary>
public class TriggerInstanceCountException : Exception
{
    /// <inheritdoc cref="TriggerInstanceCountException"/>>
    public TriggerInstanceCountException(int maximum) : base($"Allowed trigger instance maximum of {maximum} exceeded ") { }
}