using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;

/// <summary>
/// Represents the event raise, when an agv has been discovered.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="Agv">The agv.</param>
/// <param name="AgvTypeId">The type id of the agv.</param>
/// <param name="HasValidConfiguration">True, if the agv has a valid configuration.</param>
public record AgvDiscovered(Guid CorrelationId, Agv Agv, Guid? AgvTypeId, bool HasValidConfiguration = true): CorrelatedBy<Guid>;