using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to reset the offline watchdog of an agv.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
public record ResetOfflineWatchdog(Guid CorrelationId) : CorrelatedBy<Guid>;