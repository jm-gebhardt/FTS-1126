namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;

/// <summary>
/// Exception raised, when lif deletion failed
/// </summary>
public class LifDeletionFailedException : Exception
{
    /// <inheritdoc/>
    public LifDeletionFailedException(string message) : base(message)
    {
    }

    /// <inheritdoc/>
    public LifDeletionFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}