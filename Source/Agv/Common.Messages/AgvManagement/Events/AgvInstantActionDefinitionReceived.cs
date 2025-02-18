using Gebhardt.Acc.Common.Domain.Utilities;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Events;

/// <summary>
/// Represents the event raised when a new instant action definition has been received.
/// </summary>
/// <param name="AgvCorrelationId">The correlation id of the agv.</param>
/// <param name="InstantActionDefinition">The definition of the instant action.</param>
public record AgvInstantActionDefinitionReceived(Guid AgvCorrelationId, AgvInstantAction InstantActionDefinition) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{Guid}"/>>
    public Guid CorrelationId => InstantActionDefinition.ActionType.ToGuid();
}
