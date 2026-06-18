using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;

/// <summary>
/// Exception raised, when a file with length=0 was uploaded
/// </summary>
public class NoFileProvidedException : FileUploadException
{
    /// <inheritdoc/>
    public NoFileProvidedException() : base("No file provided"){}
}