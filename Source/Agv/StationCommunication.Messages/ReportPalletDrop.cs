using MassTransit;

namespace Gebhardt.Acc.StationCommunication.Messages;

/// <summary>
/// Provides a command to report the drop of a pallet.
/// </summary>
/// <param name="CorrelationId">The correlation id of the order.</param>
/// <param name="StationName">The name of the station at which the pallet is dropped.</param>
/// <param name="ConveyorLane">The target conveyor lane.</param>
/// <param name="HandlingUnit">The handling unit.</param>
public record ReportPalletDrop(Guid CorrelationId, string StationName, string ConveyorLane, string HandlingUnit) : CorrelatedBy<Guid>;