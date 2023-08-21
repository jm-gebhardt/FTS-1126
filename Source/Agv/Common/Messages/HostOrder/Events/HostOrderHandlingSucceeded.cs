using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;

/// <summary>
/// An event that reports a successful handling of an order
/// </summary>
[JetBrains.Annotations.PublicAPI]
public record HostOrderHandlingSucceeded : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <summary>
    /// The unique ID of the order known by the host system
    /// </summary>
    public string OrderId { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="HostOrderHandlingSucceeded"/>
    /// </summary>
    public HostOrderHandlingSucceeded(Guid correlationId, string orderId)
    {
        CorrelationId = correlationId;
        OrderId = orderId;
    }
}