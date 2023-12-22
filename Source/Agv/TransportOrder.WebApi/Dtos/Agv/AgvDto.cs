using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.AgvActions;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Graph;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// AGV model dto.
/// </summary>
[PublicAPI]
public record AgvDto : DatedMessage
{
    /// <summary>Is agv disturbed. There is at least one error with level "FATAL".</summary>
    public bool IsDisturbed { get; init; }

    /// <summary>Agv is in automatic mode.</summary>
    public bool IsAutomatic { get; init; }

    /// <summary>The CorrelationId of the agv.</summary>
    public Guid CorrelationId { get; init; }

    /// <summary>The agv serial number.</summary>
    public string AgvId { get; init; }

    /// <summary>The current state of the agv.</summary>
    public AgvStateType CurrentState { get; init; }

    /// <summary>Time of the last state transition.</summary>
    public DateTime TimeOfStateTransition { get; init; }

    /// <summary>Time of the last lock state change.</summary>
    public DateTime TimeOfLockStateChanged { get; init; }

    /// <summary>Communication alive state.</summary>
    public bool IsOnline { get; init; }

    /// <summary>Locked state.</summary>
    public bool IsLocked { get; init; }

    /// <summary>Battery charge level (soc).</summary>
    public double BatteryCharge { get; init; }

    /// <summary>Is Battery currently charging.</summary>
    public bool IsCharging { get; init; }

    /// <summary>Lower battery level in [%] for signaling "battery bad" (defined by VDA5050).</summary>
    public int BadBatteryLevel { get; init; }

    /// <summary>Upper battery level in [%] for signaling "battery good" (defined by VDA5050).</summary>
    public int GoodBatteryLevel { get; init; }

    /// <summary>
    /// The recent action group of the agv.
    /// </summary>
    public ActionGroupDto? CurrentActionGroup { get; init; }

    /// <summary>
    /// List of agv errors. Empty list if there is no error.
    /// </summary>
    public List<AgvErrorDto> Errors { get; set; } = new();

    /// <summary>The last node id for the agv.</summary>
    public GraphNodeDto? LastNode { get; init; }

    /// <param name="correlationId">CorrelationId.</param>
    /// <param name="agvId">AGV Serial Number.</param>
    /// <param name="currentState">Current State.</param>
    /// <param name="timeOfStateTransition">Time of the last state transition.</param>
    /// <param name ="timeOfLockStateChanged">Time of the last lock state change.</param>
    /// <param name="isOnline">Communication alive state.</param>
    /// <param name="isLocked">Locked state.</param>
    /// <param name="isDisturbed">Is agv disturbed. There is at least one error with level "FATAL".</param>
    /// <param name="isAutomatic">Agv is in automatic mode.</param>
    /// <param name="batteryCharge">Battery charge level (soc).</param>
    /// <param name="isCharging">Is battery currently charging?</param>
    /// <param name="badBatteryLevel">Lower battery level in [%] for signaling "battery bad" (defined by VDA5050).</param>
    /// <param name="goodBatteryLevel">Upper battery level in [%] for signaling "battery good" (defined by VDA5050).</param>
    /// <param name="currentActionGroup">The recent action group of the agv.</param>
    /// <param name="agvErrorDto">List of agv errors.</param>
    /// <param name="lastNode">Last node.</param>
    [PublicAPI]
    public AgvDto(Guid correlationId,
        string agvId,
        AgvStateType currentState,
        DateTime timeOfStateTransition,
        DateTime timeOfLockStateChanged,
        bool isOnline,
        bool isLocked,
        bool isDisturbed,
        bool isAutomatic,
        double batteryCharge,
        bool isCharging,
        int badBatteryLevel,
        int goodBatteryLevel,
        ActionGroupDto? currentActionGroup,
        List<AgvErrorDto> agvErrorDto,
         GraphNodeDto? lastNode)
    {
        CorrelationId = correlationId;
        AgvId = agvId;
        CurrentState = currentState;
        TimeOfStateTransition = timeOfStateTransition;
        TimeOfLockStateChanged = timeOfLockStateChanged;
        IsOnline = isOnline;
        IsLocked = isLocked;
        IsDisturbed = isDisturbed;
        IsAutomatic = isAutomatic;
        BatteryCharge = batteryCharge;
        IsCharging = isCharging;
        BadBatteryLevel = badBatteryLevel;
        GoodBatteryLevel = goodBatteryLevel;
        CurrentActionGroup = currentActionGroup;
        Errors = agvErrorDto;
        LastNode = lastNode;
    }

    /// <summary>
    /// ctor called by AutoMapper.
    /// </summary>
    public AgvDto()
    {
        AgvId = null!;
    }
}