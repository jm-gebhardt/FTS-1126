using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Exceptions;

/// <summary>
/// Represents errors that occur when files are uploaded (e.g. wrong type)
/// </summary>
[UsedImplicitly]
public class FileUploadException : Exception
{
    /// <summary>
    /// <inheritdoc cref="FileUploadException"/>
    /// </summary>
    protected FileUploadException(string description) : base(description)
    {
    }
}
