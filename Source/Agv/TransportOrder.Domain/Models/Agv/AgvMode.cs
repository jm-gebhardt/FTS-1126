using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Agv;

/// <summary>
/// The mode of the agv.
/// </summary>
[PublicAPI]
public record AgvMode
{
    /// <summary>
    /// The current mode type.
    /// </summary>
    public ModeType Type { get;  set; }

    /// <summary>
    /// The pending mode type.
    /// </summary>
    public ModeType? PendingType { get;  set; }

    /// <summary>
    /// The agv correlation id.
    /// </summary>
    public Guid AgvCorrelationId { get; set; }

    /// <summary>
    /// The optional reason for the mode.
    /// </summary>
    public string? Reason { get;   set; }

    /// <summary>
    /// The optional name of the one who set the mode.
    /// </summary>
    public string? InitiatingUser { get;   set; }

    /// <summary>
    /// The mode transition policy for the currently running action group.
    /// </summary>
    public CurrentActionGroupPolicy CurrentActionGroupPolicy { get;  set; } = CurrentActionGroupPolicy.FinishAction;

    /// <summary>
    /// The mode transition policy for assigned action groups.
    /// </summary>
    public AssignedActionGroupPolicy AssignedActionGroupPolicy { get;  set; } = AssignedActionGroupPolicy.Reset;

    /// <summary>
    /// Policy for pending manual actions: if true, cancel.
    /// </summary>
    public bool CancelManualActions { get;  set; }

    /// <inheritdoc cref="AgvMode"/>>
    public AgvMode(ModeType type, Guid agvCorrelationId, string? reason=null, string? initiatingUser=null)
    {
        Type = type;
        AgvCorrelationId = agvCorrelationId;
        Reason = reason;
        InitiatingUser = initiatingUser;
    }

    /// <inheritdoc cref="AgvMode"/>>
    public AgvMode(ModeType type,  string? reason = null, string? initiatingUser = null)
    {
        Type = type;
        AgvCorrelationId = Guid.NewGuid();
        Reason = reason;
        InitiatingUser = initiatingUser;
    }

    /// <inheritdoc cref="AgvMode"/>>
    [JsonConstructor]
    public AgvMode(ModeType type, ModeType? pendingType, Guid agvCorrelationId, string? reason, string? initiatingUser, CurrentActionGroupPolicy currentActionGroupPolicy, AssignedActionGroupPolicy assignedActionGroupPolicy, bool cancelManualActions)
    {
        Type = type;
        AgvCorrelationId = agvCorrelationId;
        Reason = reason;
        InitiatingUser = initiatingUser;
        PendingType = pendingType;
        CurrentActionGroupPolicy = currentActionGroupPolicy;
        AssignedActionGroupPolicy = assignedActionGroupPolicy;
        CancelManualActions = cancelManualActions;
    }

    /// <summary>
    /// Prepare switching to a new agv mode.
    /// </summary>
    /// <param name="newType">The new mode type.</param>
    /// <param name="reason">The optional reason for switching.</param>
    /// <param name="initiatingUser">The optional user that set of the new mode.</param>
    /// <param name="currentActionPolicy">The policy what to do with the currently running action group.</param>
    /// <param name="assignedActionPolicy">The policy what to do with assigned action groups.</param>
    /// <param name="cancelManualActions">The policy what to do with manual action groups, if true, cancel.</param>
    public void PrepareSwitch(ModeType newType, string? reason = null, string? initiatingUser = null, CurrentActionGroupPolicy currentActionPolicy = CurrentActionGroupPolicy.FinishAction, AssignedActionGroupPolicy assignedActionPolicy = AssignedActionGroupPolicy.Reset, bool cancelManualActions = false)
    {
        PendingType = newType;
        Reason = reason;
        InitiatingUser = initiatingUser;
        CurrentActionGroupPolicy = currentActionPolicy;
        AssignedActionGroupPolicy = assignedActionPolicy;
        CancelManualActions = cancelManualActions;
    }

    /// <summary>
    /// Switch to the pending agv mode.
    /// </summary>
    public void Switch()
    {
        if (PendingType == null)
        {
            return;
        }
        Type = PendingType.Value;
        PendingType = null;
    }
}

/// <summary>
/// The mode type.
/// </summary>
public enum ModeType
{
    /// <summary/>
    Normal,

    /// <summary/>
    Locked,
    
    /// <summary/>
    Maintenance,
    
    /// <summary/>
    UserMode
}

/// <summary>
/// The policy for handing the current running action group during mode transition.
/// </summary>
public enum CurrentActionGroupPolicy
{
    /// <summary/>
    FinishAction,

    /// <summary/>
    FinishActionGroup,

    /// <summary/>
    Pause
}

/// <summary>
///The policy for handing unstarted assigned action groups during mode transition.
/// </summary>
public enum AssignedActionGroupPolicy
{
    /// <summary/>
    LeaveAssigned,

    /// <summary/>
    Reset
}