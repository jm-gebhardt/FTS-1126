namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;

/// <summary>
/// Exception thrown when the serial number is not set.
/// </summary>
public class SerialNumberNotSetException : Exception
{
    /// <inheritdoc />
    public SerialNumberNotSetException(string handlingType) : base($"Serial number is not set for {handlingType}")
    {
    }
}