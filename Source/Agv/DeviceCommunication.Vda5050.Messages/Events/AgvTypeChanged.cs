using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;

/// <summary>
/// Represents the event raise, when an agv has been assigned to a different type.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="NewTypeId">The id of the new agv type.</param>
/// <param name="Agv">The agv.</param>
/// <param name="HasValidConfiguration">True, if the agv has a valid configuration.</param>
/// <param name="NeedsConfirmation">True, if the agv type change needs to be confirmed by the user.</param>
public record AgvTypeChanged(Guid CorrelationId, Guid NewTypeId, Agv Agv, bool HasValidConfiguration = true, bool NeedsConfirmation = false) : CorrelatedBy<Guid>;