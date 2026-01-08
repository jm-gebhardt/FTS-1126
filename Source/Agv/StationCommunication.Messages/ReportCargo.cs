using MassTransit;

namespace Gebhardt.Acc.StationCommunication.Messages;

/// <summary>
/// Provides a command to report the cargo state of an order.
/// </summary>
/// <param name="CorrelationId">The correlation id of the order.</param>
/// <param name="HasCargo">True if the agv has cargo.</param>
public record ReportCargo(Guid CorrelationId, bool HasCargo): CorrelatedBy<Guid>;