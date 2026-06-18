using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;

/// <summary>
/// Represents the exception raised when storing a new lif file failed
/// </summary>
public class LifFileUploadException : FileUploadException
{
    /// <inheritdoc/>
    public LifFileUploadException(string description) : base(description) {}
}