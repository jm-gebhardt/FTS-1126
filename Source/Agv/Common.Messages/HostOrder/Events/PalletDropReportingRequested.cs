using MassTransit;

namespace Gebhardt.Acc.Common.Messages.HostOrder.Events;

/// <summary>
/// Represents the event raise, when reporting the drop of a pallet is requested.
/// </summary>
/// <param name="CorrelationId">The correlation id of the order.</param>
/// <param name="StationName">The name of the station at which the pallet is dropped.</param>
/// <param name="ConveyorLane">The target conveyor lane.</param>
/// <param name="HandlingUnit">The handling unit.</param>
public record PalletDropReportingRequested(Guid CorrelationId ,string StationName, string ConveyorLane, string HandlingUnit): CorrelatedBy<Guid>;