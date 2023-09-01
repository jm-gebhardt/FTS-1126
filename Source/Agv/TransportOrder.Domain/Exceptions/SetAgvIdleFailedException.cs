namespace TransportOrder.Domain.Exceptions;

/// <summary>
/// Exception thrown when setting AGV to idle failed.
/// </summary>
public class SetAgvIdleFailedException : Exception
{
    /// <summary>
    /// Inner object of the exception.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public object InnerObject { get; }

    /// <inheritdoc cref="SetAgvIdleFailedException"/>
    public SetAgvIdleFailedException(Guid agvCorrelationId, object innerObject)
        : base($"Failed to set AGV {agvCorrelationId} to idle.")
    {
        InnerObject = innerObject;
    }
}