using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;

/// <summary>
/// Exception raised, when a wrong file type was uploaded.
/// </summary>
public class WrongFileTypeException: FileUploadException
{
    /// <inheritdoc/>
    public WrongFileTypeException(string providedFiletype, string expectedFileType):base($"Only {expectedFileType} supported, provided {providedFiletype}"){}
}