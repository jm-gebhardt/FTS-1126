using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to set the maintenance mode of an agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="InitiatingUser">The email of the user that set the mode.</param>
/// <param name="Reason">The reason for setting the mode.</param>
/// <param name="AssignedActionPolicy">The policy what to do with assigned action groups.</param>
/// <param name="CurrentActionPolicy">The policy what to do with the currently running action group.</param>
/// <param name="CancelManualActions">Cancel manual actions, if true.</param>
public record SetMaintenanceMode(Guid CorrelationId, string? InitiatingUser, string? Reason, AssignedActionGroupPolicy AssignedActionPolicy, CurrentActionGroupPolicy CurrentActionPolicy, bool CancelManualActions): CorrelatedBy<Guid>;