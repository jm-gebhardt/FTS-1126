using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

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
    /// The lock reason.
    /// </summary>
    public string Reason {get; }

    /// <summary>
    /// The email of the user who requested lock.
    /// </summary>
    public string? Setter { get; }

    /// <summary>
    /// <inheritdoc cref="LockAgv"/>
    /// </summary>
    /// <param name="correlationId">The agv correlation id.</param>
    /// <param name="reason">The lock reason.</param>
    /// <param name="setter">The email of the user who requested lock.</param>
    public LockAgv(Guid correlationId, string reason, string? setter)
    {
        CorrelationId = correlationId;
        Reason = reason;
        Setter = setter;
    }
}