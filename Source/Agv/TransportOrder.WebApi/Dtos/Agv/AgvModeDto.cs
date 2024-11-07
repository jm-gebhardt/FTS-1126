using System.Text.Json.Serialization;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// The DTO for mode of the agv.
/// </summary>
[PublicAPI]
public record AgvModeDto
{
    /// <summary>
    /// The current mode type.
    /// </summary>
    public ModeType Type { get; set; }

    /// <summary>
    /// The pending mode type.
    /// </summary>
    public ModeType? PendingType { get; set; }

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
    public string? InitiatingUser { get; set; }

    /// <summary>
    /// The mode transition policy for the currently running action group.
    /// </summary>
    public CurrentActionGroupPolicy CurrentActionGroupPolicy { get; set; } = CurrentActionGroupPolicy.FinishAction;

    /// <summary>
    /// The mode transition policy for assigned action groups.
    /// </summary>
    public AssignedActionGroupPolicy AssignedActionGroupPolicy { get; set; } = AssignedActionGroupPolicy.Reset;

    /// <summary>
    /// Policy for pending manual actions: if true, cancel.
    /// </summary>
    public bool CancelManualActions { get; set; }

    /// <summary>
    /// The date when mode change was requested.
    /// </summary>
    public DateTime? ChangeTime { get; set; }

    /// <inheritdoc cref="AgvModeDto"/>>
    [JsonConstructor]
    public AgvModeDto(
        ModeType type,
        ModeType? pendingType,
        Guid agvCorrelationId,
        string? initiatingUser,
        CurrentActionGroupPolicy currentActionGroupPolicy,
        AssignedActionGroupPolicy assignedActionGroupPolicy,
        bool cancelManualActions,
        LockReason? lockReason = null,
        MaintenanceReason? maintenanceReason = null,
        string? reasonDescription = null,
        DateTime? changeTime = null
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
        ChangeTime = changeTime;
    }

    /// <inheritdoc cref="AgvModeDto"/>>
    public AgvModeDto(
        ModeType type,
        Guid agvCorrelationId,
        LockReason? lockReason = null,
        MaintenanceReason? maintenanceReason = null,
        string? reasonDescription = null,
        string? initiatingUser = null,
        DateTime? changeTime = null)
    {
        Type = type;
        AgvCorrelationId = agvCorrelationId;
        LockReason = lockReason;
        MaintenanceReason = maintenanceReason;
        ReasonDescription = reasonDescription;
        InitiatingUser = initiatingUser;
        ChangeTime = changeTime;
    }

    /// <summary>
    /// ctor called by AutoMapper.
    /// </summary>
    public AgvModeDto()
    {
    }
}