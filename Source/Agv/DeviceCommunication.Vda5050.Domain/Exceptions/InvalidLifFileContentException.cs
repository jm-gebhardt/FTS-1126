using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;

/// <summary>
/// Exception raised, when the content of the lif file does not contain expected data
/// </summary>
public class InvalidLifFileContentException : FileUploadException
{
    /// <inheritdoc/>
    public InvalidLifFileContentException(): base("File content is not lif layout collection"){}

    /// <inheritdoc/>
    public InvalidLifFileContentException(string details) : base($"File content is not lif layout collection:{details}") { }
}