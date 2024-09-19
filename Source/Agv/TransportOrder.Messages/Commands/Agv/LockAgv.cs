using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Represents the LockAgv request
/// </summary>
[PublicAPI]
public record LockAgv : CorrelatedBy<Guid>
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// The lock reason.
    /// </summary>
    public string Reason {get; }

    /// <summary>
    /// The email of the user who requested lock.
    /// </summary>
    public string? InitiatingUser { get; }

    /// <summary>
    /// The policy what to do with assigned action groups
    /// </summary>
    public AssignedActionGroupPolicy AssignedActionPolicy { get; set; }

    /// <summary>
    /// The policy what to do with the manual actions, if true, cancel.
    /// </summary>
    public CurrentActionGroupPolicy CurrentActionPolicy { get; set; }

    /// <summary>
    /// The policy what to do with the currently running action group.
    /// </summary>
    public bool CancelManualActions { get; set; }

    /// <inheritdoc cref="LockAgv"/>
    public LockAgv(Guid correlationId, string reason, string? initiatingUser, AssignedActionGroupPolicy assignedActionPolicy, CurrentActionGroupPolicy currentActionPolicy, bool cancelManualActions)
    {
        CorrelationId = correlationId;
        Reason = reason;
        InitiatingUser = initiatingUser;
        AssignedActionPolicy = assignedActionPolicy;
        CurrentActionPolicy = currentActionPolicy;
        CancelManualActions = cancelManualActions;
    }
}