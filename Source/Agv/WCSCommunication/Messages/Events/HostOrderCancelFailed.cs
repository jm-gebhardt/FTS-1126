
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.WCSCommunication.Messages.Events;

/// <summary>
/// An event that reports a failed cancel of an order
/// </summary>
[PublicAPI]
public record HostOrderCancelFailed : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <summary>
    /// The unique ID of the order known by the host system
    /// </summary>
    public string OrderId { get; }

    /// <summary>
    /// The error code that holds information why cancellation of the order failed
    /// </summary>
    public HostOrderCancelFailedErrorCode Error { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="HostOrderCancelFailed"/>
    /// </summary>
    public HostOrderCancelFailed(Guid correlationId, string orderId, HostOrderCancelFailedErrorCode error)
    {
        CorrelationId = correlationId;
        OrderId = orderId;
        Error = error;
    }
}