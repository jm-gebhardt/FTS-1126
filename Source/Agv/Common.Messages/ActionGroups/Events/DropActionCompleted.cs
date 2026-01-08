using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Events;

/// <summary>
/// Represents the event raised when a drop action has been completed.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
/// <param name="ActionCorrelationId">The correlation id of the drop action.</param>
/// <param name="ActionGroup">The correlation of the assigned agv.</param>
/// <param name="Timestamp">The time of completion.</param>
[PublicAPI]
public record DropActionCompleted(Guid CorrelationId, Guid ActionCorrelationId, ActionGroup ActionGroup, DateTime Timestamp) : CorrelatedBy<Guid>;