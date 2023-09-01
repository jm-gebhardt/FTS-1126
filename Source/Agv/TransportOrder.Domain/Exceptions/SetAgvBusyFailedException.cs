namespace TransportOrder.Domain.Exceptions;

/// <summary>
/// Exception thrown when setting AGV to busy failed.
/// </summary>
public class SetAgvBusyFailedException : Exception
{
    /// <summary>
    /// Inner object of the exception.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public object InnerObject { get; }

    /// <inheritdoc cref="SetAgvIdleFailedException"/>
    public SetAgvBusyFailedException(Guid agvCorrelationId, object innerObject)
        : base($"Failed to set AGV {agvCorrelationId} to busy.")
    {
        InnerObject = innerObject;
    }
}