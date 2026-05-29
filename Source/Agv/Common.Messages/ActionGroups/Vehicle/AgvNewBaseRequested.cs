using Gebhardt.Acc.Common.Messages.Contracts;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Vehicle;

/// <summary>Represents an event raised when an AGV signals a new-base request.</summary>
[PublicAPI]
public record AgvNewBaseRequested : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="IAgv.CorrelationId"/>
    public Guid CorrelationId { get; }

    /// <inheritdoc cref="AgvNewBaseRequested"/>
    public AgvNewBaseRequested(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}