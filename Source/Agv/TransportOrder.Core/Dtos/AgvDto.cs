using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos
{
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
    /// <param name="CorrelationId">CorrelationId</param>
    /// <param name="AgvId">AGV Serial Number</param>
    /// <param name="CurrentState">Current State</param>
    /// <param name="TimeOfStateTransition">Time of the last state transition</param>
    /// <param name ="TimeOfLockStateChanged">Time of the last lock state change</param>
    /// <param name="IsOnline">Communication alive state</param>
    /// <param name="IsLocked">Locked state</param>
    /// <param name="IsDisturbed">Is agv disturbed. There is at least one error with level "FATAL"</param>
    /// <param name="IsAutomatic">AGV is in automatic mode</param>
    /// <param name="BatteryCharge">Battery charge level (Soc)</param>
    /// <param name="IsCharging">Is Battery currently charging</param>
    /// <param name="BadBatteryLevel">Lower battery level in [%] for signaling "battery bad" (defined by VDA5050)</param>
    /// <param name="GoodBatteryLevel">Upper battery level in [%] for signaling "battery good" (defined by VDA5050)</param>
    /// <param name="CurrentActionGroup"></param>
    [PublicAPI]
    public record AgvDto(
        Guid CorrelationId, 
        string AgvId,
        AgvState CurrentState,
        DateTime TimeOfStateTransition,
        DateTime TimeOfLockStateChanged,
        bool IsOnline, 
        bool IsLocked,
        bool IsDisturbed,
        bool IsAutomatic,
        // Battery
        double BatteryCharge,
        bool IsCharging,
        int BadBatteryLevel,
        int GoodBatteryLevel,
        // Action
        ActionGroupDto? CurrentActionGroup
        );
}
