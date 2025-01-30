using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to pause an action group.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
/// <param name="PauseReason">The reason for the pause.</param>
public record PauseActionGroup(Guid CorrelationId, PauseReason PauseReason = PauseReason.None) : CorrelatedBy<Guid>;