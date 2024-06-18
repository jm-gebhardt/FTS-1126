using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Represents the event raised when the mode of an agv changed.
/// </summary>
/// <param name="CorrelationId">The correlationId of the agv.</param>
/// <param name="Mode">The new agv mode.</param>
/// <param name="CancelManualActions">The cancel all manual actions for the agv.</param>
public record AgvModeChanged(Guid CorrelationId, AgvMode Mode, bool CancelManualActions=false): CorrelatedBy<Guid>;