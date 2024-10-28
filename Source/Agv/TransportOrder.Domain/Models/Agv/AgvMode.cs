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
    /// Optional reason for locking an AGV
    /// </summary>
    public LockReason? LockReason { get; set; }

    /// <summary>
    /// Optional reason for setting maintenance mode on an AGV
    /// </summary>
    public MaintenanceReason? MaintenanceReason { get; set; }

    /// <summary>
    /// Optional description for the reason for the mode.
    /// </summary>
    public string? ReasonDescription { get; set; }

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
    public AgvMode(
        ModeType type, 
        Guid agvCorrelationId, 
        LockReason? lockReason = null,
        MaintenanceReason? maintenanceReason = null,
        string? reasonDescription=null, 
        string? initiatingUser=null)
    {
        Type = type;
        AgvCorrelationId = agvCorrelationId;
        LockReason = lockReason;
        MaintenanceReason = maintenanceReason;
        ReasonDescription = reasonDescription;
        InitiatingUser = initiatingUser;
    }

    /// <inheritdoc cref="AgvMode"/>>
    public AgvMode(
        ModeType type,
        LockReason? lockReason = null,
        MaintenanceReason? maintenanceReason = null,
        string? reasonDescription = null, 
        string? initiatingUser = null)
    {
        Type = type;
        AgvCorrelationId = Guid.NewGuid();
        LockReason = lockReason;
        MaintenanceReason = maintenanceReason;
        ReasonDescription = reasonDescription;
        InitiatingUser = initiatingUser;
    }

    /// <inheritdoc cref="AgvMode"/>>
    [JsonConstructor]
    public AgvMode(
        ModeType type, 
        ModeType? pendingType, 
        Guid agvCorrelationId, 
        string? initiatingUser, 
        CurrentActionGroupPolicy currentActionGroupPolicy, 
        AssignedActionGroupPolicy assignedActionGroupPolicy, 
        bool cancelManualActions,
        //string? reason=null,
        LockReason? lockReason = null,
        MaintenanceReason? maintenanceReason = null,
        string? reasonDescription = null
        )
    {
        Type = type;
        AgvCorrelationId = agvCorrelationId;
        LockReason = lockReason;
        MaintenanceReason = maintenanceReason;
        ReasonDescription = reasonDescription;
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
    /// <param name="lockReason">The optional reason for locking.</param>
    /// <param name="maintenanceReason">The optional reason for maintenance.</param>
    /// <param name="reasonDescription">The optional reason description for switching modes.</param>
    /// <param name="initiatingUser">The optional user that set of the new mode.</param>
    /// <param name="currentActionPolicy">The policy what to do with the currently running action group.</param>
    /// <param name="assignedActionPolicy">The policy what to do with assigned action groups.</param>
    /// <param name="cancelManualActions">The policy what to do with manual action groups, if true, cancel.</param>
    public void PrepareSwitch(
        ModeType newType, 
        LockReason? lockReason = null,
        MaintenanceReason? maintenanceReason = null,
        string? reasonDescription = null, 
        string? initiatingUser = null, 
        CurrentActionGroupPolicy currentActionPolicy = CurrentActionGroupPolicy.FinishAction, 
        AssignedActionGroupPolicy assignedActionPolicy = AssignedActionGroupPolicy.Reset, 
        bool cancelManualActions = false)
    {
        PendingType = newType;
        LockReason = lockReason;
        MaintenanceReason= maintenanceReason;
        ReasonDescription = reasonDescription;
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
/// Reason for locking
/// </summary>
[PublicAPI]
public enum LockReason
{
    /// <summary>
    /// 
    /// </summary>
    NavigationError = 0,

    /// <summary/>
    SensorFailures,

    /// <summary/>
    HardwareAdjustments,
    
    /// <summary/>
    OperationalAdjustments,

    /// <summary/>
    SystemReboot,

    /// <summary/>
    PreventiveMaintenance,

    /// <summary/>
    SoftwareFirmwareUpdate,

    /// <summary/>
    MechanicalFailure,

    /// <summary/>
    BatteryIssues,

    /// <summary/>
    SensorMalfunctions,

    /// <summary/>
    WheelsTiresWearingOut,

    /// <summary/>
    Other
}

/// <summary>
/// Reason for maintenance
/// </summary>
[PublicAPI]
public enum MaintenanceReason
{
    /// <summary/>
    PreventiveMaintenance,

    /// <summary/>
    SoftwareFirmwareUpdate,

    /// <summary/>
    MechanicalFailure,

    /// <summary/>
    BatteryIssues,

    /// <summary/>
    SensorMalfunctions,

    /// <summary/>
    WheelsTiresWearingOut,

    /// <summary/>
    Other
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