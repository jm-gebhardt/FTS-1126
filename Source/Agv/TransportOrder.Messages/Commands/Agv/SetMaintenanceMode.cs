using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to set the maintenance mode of an agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="Setter">The email of the user that set the mode.</param>
/// <param name="Reason">The reason for setting the mode.</param>
/// <param name="CancelManualActions">Cancel manual actions, if true.</param>
public record SetMaintenanceMode(Guid CorrelationId, string? Setter, string? Reason, bool CancelManualActions): CorrelatedBy<Guid>;