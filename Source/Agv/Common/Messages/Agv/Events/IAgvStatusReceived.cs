using MassTransit;


namespace Gebhardt.StoreWare.Agv.Common.Messages.Agv.Events
{
    /// <summary>
    /// An event reporting the current status of an AGV.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public interface IAgvStatusReceived : CorrelatedBy<Guid>
    {
        /// <summary>
        /// Lower battery level in [%] for signaling "battery bad" (defined by VDA5050).
        /// </summary>
        public const int BadBatteryLevel = 20;
        /// <summary>
        /// Upper battery level in [%] for signaling "battery good" (defined by VDA5050).
        /// </summary>
        public const int GoodBatteryLevel = 80;


        /// <summary>
        /// Identifier of the AGV.
        /// </summary>
        string AgvId { get; }

        /// <summary>
        /// Current or previous order ID.
        /// </summary>
        /// <remarks>
        /// VDA5050.state.schema.orderId
        /// The order ID is kept until a new order is received.
        /// </remarks>
        string? OrderId { get; }

        /// <summary>
        /// AGV is busy (not idle).
        /// </summary>
        /// <remarks>
        /// VDA5050.state.schema.actionStates.actionStatus 
        /// There is at least one action pending (neither finished no failed).
        /// </remarks>
        bool Busy { get; }

        /// <summary>
        /// AGV is in automatic mode.
        /// </summary>
        /// <remarks>
        /// VDA5050.state.schema.operatingMode 
        /// Only mode "AUTOMATIC" is regarded.
        /// </remarks>
        bool Automatic { get; }

        /// <summary>
        /// Battery charge.
        /// </summary>
        /// <remarks>
        /// VDA5050.state.schema.batteryState.batteryCharge
        /// Battery charge level in [%].
        /// <para>Less than or equal <see cref="BadBatteryLevel"/>% is regarded as "battery low", charging required.</para>
        /// </remarks>
        int BatteryCharge { get; }

        /// <summary>
        /// AGV is charging.
        /// </summary>
        /// <remarks>
        /// VDA5050.state.schema.batteryState.charging
        /// Charging in progress.
        /// </remarks>
        bool Charging { get; }

        /// <summary>
        /// AGV is disturbed.
        /// </summary>
        /// <remarks>
        /// VDA5050.state.schema.errors.errorLevel
        /// There is at least one error with level "FATAL".
        /// </remarks>
        bool Disturbed { get; }
    }
}