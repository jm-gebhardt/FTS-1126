using MassTransit;

namespace TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Represents the LockAgv request
/// </summary>
public record LockAgv : CorrelatedBy<Guid>
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// <inheritdoc cref="LockAgv"/>
    /// </summary>
    /// <param name="correlationId"></param>
    public LockAgv(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}