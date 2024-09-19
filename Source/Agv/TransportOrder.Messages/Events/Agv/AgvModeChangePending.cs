using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Represents the event raised when an agv mode change is pending.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="Mode">The agv mode.</param>
public record AgvModeChangePending(Guid CorrelationId, AgvMode Mode) : CorrelatedBy<Guid>;