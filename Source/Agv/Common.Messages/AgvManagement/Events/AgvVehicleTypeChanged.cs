using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Events;

/// <summary>
/// Represents the event raised when the vehicle type of the agv has been changed.
/// </summary>
/// <param name="CorrelationId">Correlation id of the agv.</param>
/// <param name="SerialNumber">Serial number of the agv.</param>
/// <param name="NewTypeId">Id of the new type.</param>
/// <param name="NewVehicleType">Vehicle type id of the new type.</param>
/// <param name="HasValidConfiguration">True, if the agv has a valid configuration.</param>
/// <param name="NeedsConfirmation">True, if the agv type change needs to be confirmed by a user.</param>
public record AgvVehicleTypeChanged(Guid CorrelationId, string SerialNumber, Guid NewTypeId, string NewVehicleType, bool HasValidConfiguration=true, bool NeedsConfirmation=false) : CorrelatedBy<Guid>;