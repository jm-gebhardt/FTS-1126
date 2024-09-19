using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to set the user mode of an agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="InitiatingUser">The email of the user that set the mode.</param>
/// <param name="AssignedActionPolicy">The policy what to do with assigned action groups.</param>
/// <param name="CurrentActionPolicy">The policy what to do with the currently running action group.</param>
/// <param name="CancelManualActions">The policy what to do with the manual actions, if true, cancel.</param>
public record SetUserMode(Guid CorrelationId, string? InitiatingUser, AssignedActionGroupPolicy AssignedActionPolicy, CurrentActionGroupPolicy CurrentActionPolicy, bool CancelManualActions) : CorrelatedBy<Guid>;