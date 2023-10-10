using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;

/// <summary>
/// Exception thrown when setting AGV to idle failed.
/// </summary>
[UsedImplicitly]
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