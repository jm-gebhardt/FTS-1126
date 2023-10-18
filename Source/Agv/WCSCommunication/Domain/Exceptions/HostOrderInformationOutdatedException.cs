namespace Gebhardt.StoreWare.Agv.WCSCommunication.Domain.Exceptions;

/// <summary>
/// Represents the exception thrown when order information are outdated.
/// </summary>
public class HostOrderInformationOutdatedException : Exception
{
    /// <inheritdoc cref="HostOrderInformationOutdatedException"/>
    public HostOrderInformationOutdatedException(string message) : base(message)
    {
    }
}