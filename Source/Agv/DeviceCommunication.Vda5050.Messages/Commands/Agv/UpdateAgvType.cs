using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Agv;

/// <summary>
/// Provides a command to update an agv type.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="NewTypeId">The id of the new agv type.</param>
/// <param name="Agv">The agv.</param>
/// <param name="HasValidConfiguration">True, if the agv has a valid configuration.</param>
/// <param name="NeedsConfirmation">True, if the agv type change needs to be confirmed by the user.</param>
public record UpdateAgvType(Guid CorrelationId, Guid NewTypeId, Domain.Models.Agvs.Agv Agv, bool HasValidConfiguration = true, bool NeedsConfirmation=false) : CorrelatedBy<Guid>;