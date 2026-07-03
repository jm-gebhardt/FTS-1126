namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;

/// <summary>
/// Represents the exception raised when an AGV type with the same identifier already exists.
/// </summary>
public class AgvTypeAlreadyExistsException : Exception
{
    /// <inheritdoc cref="AgvTypeAlreadyExistsException"/>
    public AgvTypeAlreadyExistsException(string agvTypeIdentifier)
        : base($"An AGV type with identifier '{agvTypeIdentifier}' already exists.")
    {
    }
}