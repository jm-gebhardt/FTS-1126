using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to update the agv type of an agv.
/// </summary>
/// <param name="CorrelationId">Correlation id of the agv.</param>
/// <param name="SerialNumber">Serial number of the agv.</param>
/// <param name="AgvTypeId">Id of the new type.</param>
/// <param name="AgvTypeName">Vehicle type id of the new type.</param>
/// <param name="HasValidConfiguration">True, if the agv has a valid configuration.</param>
/// <param name="NeedsConfirmation">True, if the agv type change needs to be confirmed by a user.</param>
public record UpdateAgvType(Guid CorrelationId, string SerialNumber, Guid AgvTypeId, string AgvTypeName, bool HasValidConfiguration = true, bool NeedsConfirmation = false) : CorrelatedBy<Guid>;