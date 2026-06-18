using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Exceptions;

/// <summary>
/// Represents the exception raised, when a file could not be found.
/// </summary>
[UsedImplicitly]
public class FileNotFoundException : NotFoundException
{
    /// <inheritdoc/>
    public FileNotFoundException(Guid id):base("file", "id", id){}
}