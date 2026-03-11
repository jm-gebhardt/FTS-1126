namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;

/// <summary>
/// Represents the exception raised when a required agv action parameter from a predefined VDA5050 action has not been provided.
/// </summary>
public class RequiredAgvActionParameterMissing: Exception
{
    /// <inheritdoc />
    public RequiredAgvActionParameterMissing(string actionType) : base($"Required parameters not set for agv action '{actionType}'")
    {
    }
}