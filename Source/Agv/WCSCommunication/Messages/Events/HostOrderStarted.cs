using MassTransit;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Messages.Events;

/// <summary>
/// An event that reports the successful start of a transport order by the specified AGV
/// </summary>
public record HostOrderStarted : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <summary>
    /// The unique ID of the order known by the host system
    /// </summary>
    public string OrderId { get; }

    /// <summary>
    /// The assigned AGV
    /// </summary>
    public string AssignedAgv { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="HostOrderStarted"/>
    /// </summary>
    public HostOrderStarted(Guid correlationId, string orderId, string assignedAgv)
    {
        CorrelationId = correlationId;
        OrderId = orderId;
        AssignedAgv = assignedAgv;
    }
}