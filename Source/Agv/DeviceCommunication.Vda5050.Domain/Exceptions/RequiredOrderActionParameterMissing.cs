namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;

/// <summary>
/// Represents the exception raised when a required order action parameter has not been provided.
/// </summary>
public class RequiredOrderActionParameterMissing: Exception
{
    /// <inheritdoc />
    public RequiredOrderActionParameterMissing(string parameterKey, string actionType) : base($"Required parameter '{parameterKey}' not set for order action '{actionType}'")
    {
    }
}