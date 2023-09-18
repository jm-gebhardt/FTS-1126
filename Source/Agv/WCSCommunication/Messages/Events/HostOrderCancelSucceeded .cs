using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.WCSCommunication.Messages.Events;

/// <summary>
/// An event that reports a successful cancel of an order
/// </summary>
[PublicAPI]
public record HostOrderCancelSucceeded : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <summary>
    /// The unique ID of the order known by the host system
    /// </summary>
    public string OrderId { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="HostOrderCancelSucceeded"/>
    /// </summary>
    public HostOrderCancelSucceeded(Guid correlationId, string orderId)
    {
        CorrelationId = correlationId;
        OrderId = orderId;
    }
}