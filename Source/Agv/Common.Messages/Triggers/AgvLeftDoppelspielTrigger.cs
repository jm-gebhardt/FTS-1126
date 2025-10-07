using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Triggers;

/// <summary>
/// Represents the event raised when an agv left the Doppelspieltrigger
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="TargetStationName">The name of the target station of the Doppelspiel.</param>
public record AgvLeftDoppelspielTrigger(Guid CorrelationId, string TargetStationName): CorrelatedBy<Guid>;