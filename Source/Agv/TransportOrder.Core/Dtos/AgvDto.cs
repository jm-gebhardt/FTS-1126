using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos.AgvActions;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;

/// <summary>
/// Agv State
/// </summary>
public enum AgvState
{
    /// <summary>
    /// Initial
    /// </summary>
    [UsedImplicitly]
    Initial,

    /// <summary>
    /// Discovered
    /// </summary>
    [UsedImplicitly]
    Discovered,

    /// <summary>
    /// Idle
    /// </summary>
    [UsedImplicitly]
    Idle,

    /// <summary>
    /// Busy
    /// </summary>
    [UsedImplicitly]
    Busy,

    /// <summary>
    /// Charging
    /// </summary>
    [UsedImplicitly]
    Charging
};

/// <summary>
/// AGV model
/// </summary>
[PublicAPI]
public record AgvDto
{
    /// <summary>Is agv disturbed. There is at least one error with level "FATAL"</summary>
    public bool IsDisturbed { get; init; }

    /// <summary>AGV is in automatic mode</summary>
    public bool IsAutomatic { get; init; }

    /// <summary>The CorrelationId of the Agv</summary>
    public Guid CorrelationId { get; init; }

    /// <summary>AGV Serial Number</summary>
    public string AgvId { get; init; }

    /// <summary>Current State</summary>
    public AgvState CurrentState { get; init; }

    /// <summary>Time of the last state transition</summary>
    public DateTime TimeOfStateTransition { get; init; }

    /// <summary>Time of the last lock state change</summary>
    public DateTime TimeOfLockStateChanged { get; init; }

    /// <summary>Communication alive state</summary>
    public bool IsOnline { get; init; }

    /// <summary>Locked state</summary>
    public bool IsLocked { get; init; }

    /// <summary>Battery charge level (Soc)</summary>
    public double BatteryCharge { get; init; }

    /// <summary>Is Battery currently charging</summary>
    public bool IsCharging { get; init; }

    /// <summary>Lower battery level in [%] for signaling "battery bad" (defined by VDA5050)</summary>
    public int BadBatteryLevel { get; init; }

    /// <summary>Upper battery level in [%] for signaling "battery good" (defined by VDA5050)</summary>
    public int GoodBatteryLevel { get; init; }

    /// <summary>
    /// The recent ActionGroup of the Agv
    /// </summary>
    public ActionGroupDto? CurrentActionGroup { get; init; }

    /// <summary>
    /// List of Agv errors. Empty List if there is no error.
    /// </summary>
    public List<AgvErrorDto> Errors { get; set; } = new();

    /// <param name="correlationId">CorrelationId</param>
    /// <param name="agvId">AGV Serial Number</param>
    /// <param name="currentState">Current State</param>
    /// <param name="timeOfStateTransition">Time of the last state transition</param>
    /// <param name ="timeOfLockStateChanged">Time of the last lock state change</param>
    /// <param name="isOnline">Communication alive state</param>
    /// <param name="isLocked">Locked state</param>
    /// <param name="isDisturbed">Is agv disturbed. There is at least one error with level "FATAL"</param>
    /// <param name="isAutomatic">AGV is in automatic mode</param>
    /// <param name="batteryCharge">Battery charge level (Soc)</param>
    /// <param name="isCharging">Is Battery currently charging</param>
    /// <param name="badBatteryLevel">Lower battery level in [%] for signaling "battery bad" (defined by VDA5050)</param>
    /// <param name="goodBatteryLevel">Upper battery level in [%] for signaling "battery good" (defined by VDA5050)</param>
    /// <param name="currentActionGroup">The recent ActionGroup of the Agv</param>
    /// <param name="agvErrorDto">List of Agv errors</param>
    [PublicAPI]
    public AgvDto(
        Guid correlationId,
        string agvId,
        AgvState currentState,
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
        List<AgvErrorDto> agvErrorDto
    )
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
    }

    /// <summary>
    /// AutoMapper ctor
    /// </summary>
    public AgvDto()
    {
        AgvId = null!;
    }
}