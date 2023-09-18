using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;

/// <summary>
/// An event that reports a failed order update
/// </summary>
[PublicAPI]
public record HostOrderUpdateFailed : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <summary>
    /// The unique ID of the order known by the host system
    /// </summary>
    public string OrderId { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="HostOrderUpdateFailed"/>
    /// </summary>
    public HostOrderUpdateFailed(Guid correlationId, string orderId)
    {
        CorrelationId = correlationId;
        OrderId = orderId;
    }
}