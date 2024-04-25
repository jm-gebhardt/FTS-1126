using Gebhardt.Acc.Common.Domain.Models;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Events;

/// <summary>
/// An event reporting the error status of Agv
/// </summary>
public record AgvErrorStatusReceived : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="AgvErrorStatusReceived"/>
    public AgvErrorStatusReceived(Guid correlationId, List<AgvError>? errors, AgvPosition agvPosition)
    {
        CorrelationId = correlationId;
        Errors = errors;
        AgvPosition = agvPosition;
    }

    /// <inheritdoc />
    public Guid CorrelationId { get; }

    /// <summary>
    /// List of errors, Agv had encountered
    /// </summary>
    public List<AgvError>? Errors { get; set; }

    /// <summary>
    /// Current AGV position
    /// </summary>
    public AgvPosition AgvPosition { get; }
}