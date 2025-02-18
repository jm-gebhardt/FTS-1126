namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Exception raised when a given parameter is invalid for this instant action.
/// </summary>
public class InvalidInstantActionParameterException : Exception
{
    /// <inheritdoc cref="InvalidInstantActionParameterException"/>>
    public InvalidInstantActionParameterException(string parameterKey, string actionType) : base($"Parameter {parameterKey} invalid for action {actionType}"){}
}