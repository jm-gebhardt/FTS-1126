using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Messages.Events;

/// <summary>
/// An event that reports a failure while handling an order
/// </summary>
[PublicAPI]
public record HostOrderHandlingFailed : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <summary>
    /// The unique ID of the order known by the host system
    /// </summary>
    public string OrderId { get; }

    /// <summary>
    /// The error code that holds information why the handling of the order failed
    /// </summary>
    public HostOrderHandlingFailedErrorCode Error { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="HostOrderHandlingFailedErrorCode"/>
    /// </summary>
    public HostOrderHandlingFailed(Guid correlationId, string orderId, HostOrderHandlingFailedErrorCode error)
    {
        CorrelationId = correlationId;
        OrderId = orderId;
        Error = error;
    }
}