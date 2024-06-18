namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represents the exception raised when an action is not allowed because the agv is in the wrong mode.
/// </summary>
public class ActionNotAllowedInModeException : Exception
{
    /// <inheritdoc cref="ActionNotAllowedInModeException"/>>
    public ActionNotAllowedInModeException(Guid agvCorrelationId, string mode): base($"Action not allowed: agv {agvCorrelationId} is in mode {mode}.")
    {
    }
}