using FileNotFoundException = Gebhardt.Acc.Common.Domain.Exceptions.FileNotFoundException;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;

/// <summary>
/// Represents the exception raised, when a lif file could not be found.
/// </summary>
public class LifFileNotFoundException : FileNotFoundException
{
    /// <inheritdoc/>
    public LifFileNotFoundException(Guid id):base(id){}
}