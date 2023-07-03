using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

/// <summary>
/// An event reporting the error status of Agv
/// </summary>
public record AgvErrorStatusReceived : CorrelatedBy<Guid>
{
    /// <summary>
    /// Initializes a new instance of <see cref="AgvErrorStatusReceived"/>
    /// </summary>
    /// <param name="correlationId">The Agv CorrelationId</param>
    /// <param name="errors"></param>
    public AgvErrorStatusReceived(Guid correlationId, List<AgvError>? errors)
    {
        CorrelationId = correlationId;
        Errors = errors;
    }

    /// <inheritdoc />
    public Guid CorrelationId { get; }

    /// <summary>
    /// List of errors, Agv had encountered
    /// </summary>
    public List<AgvError>? Errors { get; set; }
}