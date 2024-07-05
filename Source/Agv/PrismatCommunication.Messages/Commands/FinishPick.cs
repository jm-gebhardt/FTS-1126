using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Messages.Commands;

/// <summary>
/// Provides a command to report completion of a pick action.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
/// <param name="ActionCorrelationId">The correlation id of the pick action.</param>
/// <param name="Timestamp">The timestamp of completion.</param>
public record FinishPick(Guid CorrelationId, Guid ActionCorrelationId, DateTime Timestamp) : CorrelatedBy<Guid>;