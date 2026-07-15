using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Agv;

/// <summary>
/// Provides a command to add an agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="Agv">The agv.</param>
/// <param name="AgvTypeId">The type id of the agv.</param>
/// <param name="HasValidConfiguration">True, if the agv has a valid configuration.</param>
public record AddAgv(Guid CorrelationId, Domain.Models.Agvs.Agv Agv, Guid? AgvTypeId=null, bool HasValidConfiguration = true) : CorrelatedBy<Guid>;