using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement
{
    /// <summary>
    /// AGV Status
    /// </summary>
    public record AgvStatus : CorrelatedBy<Guid>
    {
        /// <summary>
        ///  AGV correlation ID
        /// </summary>
        [UsedImplicitly]
        public Guid CorrelationId { get; }

        /// <summary>
        /// AGV is busy (not idle).
        /// </summary>
        /// <remarks>
        /// VDA5050.state.schema.actionStates.actionStatus 
        /// There is at least one action pending (neither finished no failed).
        /// </remarks>
        [UsedImplicitly]
        public bool Busy { get; }

        /// <summary>
        /// AGV is in automatic mode.
        /// </summary>
        /// <remarks>
        /// VDA5050.state.schema.operatingMode 
        /// Only mode "AUTOMATIC" is regarded.
        /// </remarks>
        [UsedImplicitly]
        public bool Automatic { get; }

        /// <summary>
        /// Battery charge.
        /// </summary>
        /// <remarks>
        /// VDA5050.state.schema.batteryState.batteryCharge
        /// Battery charge level in [%].
        /// <para>Less than or equal <see cref="BadBatteryLevel"/>% is regarded as "battery low", charging required.</para>
        /// </remarks>
        [UsedImplicitly]
        public double BatteryCharge { get; }

        /// <summary>
        /// AGV is charging.
        /// </summary>
        /// <remarks>
        /// VDA5050.state.schema.batteryState.charging
        /// Charging in progress.
        /// </remarks>
        [UsedImplicitly]
        public bool Charging { get; }

        /// <summary>
        /// AGV is disturbed.
        /// </summary>
        /// <remarks>
        /// VDA5050.state.schema.errors.errorLevel
        /// There is at least one error with level "FATAL".
        /// </remarks>
        [UsedImplicitly]
        public bool Disturbed { get; }

        /// <summary>
        /// Lower battery level in [%] for signaling "battery bad" (defined by VDA5050).
        /// </summary>
        [UsedImplicitly]
        public const int BadBatteryLevel = 20;
        
        /// <summary>
        /// Upper battery level in [%] for signaling "battery good" (defined by VDA5050).
        /// </summary>
        [UsedImplicitly]
        public const int GoodBatteryLevel = 80;

        /// <summary>
        /// nodeID of last reached node or, if AGV is currently on a node, current node.
        /// Empty string if no lastNodeId is available.
        /// </summary>
        /// <remarks>
        /// VDA5050.state.schema.lastNodeId
        /// </remarks>
        [UsedImplicitly]
        public string? LastNodeId { get; }


        /// <summary>
        /// Constructor of AgvStatus
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="busy"></param>
        /// <param name="automatic"></param>
        /// <param name="batteryCharge"></param>
        /// <param name="charging"></param>
        /// <param name="disturbed"></param>
        /// <param name="lastNodeId"></param>
        public AgvStatus(Guid correlationId, bool busy, bool automatic, double batteryCharge, bool charging, bool disturbed, string? lastNodeId = null)
        {
            CorrelationId = correlationId;
            Busy = busy;
            Automatic = automatic;
            BatteryCharge = batteryCharge;
            Charging = charging;
            Disturbed = disturbed;
            LastNodeId = lastNodeId;
        }
    }
}
