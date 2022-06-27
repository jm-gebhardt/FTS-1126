using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement
{
    /// <summary>
    /// AGV states.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public enum AgvState
    {
        /// <summary>
        /// New AGV discovered by receiving IAgvStatusReceived while missing information evaluating the current state of the AGV.
        /// </summary>
        /// <remarks>Hypothetical, receiving <see cref="IAgvStatusReceived"/> will lead to one of the subsequent states.</remarks>
        Discovered,

        /// <summary>
        /// Idle.
        /// </summary>
        Idle,

        /// <summary>
        /// AGV is reserved for an assignment to an order.
        /// </summary>
        /// <remarks>This state is used prevent multiple assignments of the same AGV</remarks>
        Reserved,

        /// <summary>
        /// Busy.
        /// </summary>
        Busy,

        /// <summary>
        /// Busy, maintenance requested.
        /// </summary>
        /// <remarks>Reserved, for future use.</remarks>
        BusyRequestForMaintenance,

        /// <summary>
        /// Busy, charging requested.
        /// </summary>
        BusyRequestForCharging,

        /// <summary>
        /// Disturbed.
        /// </summary>
        Error,

        /// <summary>
        /// Charging.
        /// </summary>
        Charging,

        /// <summary>
        /// Maintaining.
        /// </summary>
        Maintenance,

        /// <summary>
        /// Disabled.
        /// </summary>
        /// <remarks>Reserved, for future use.</remarks>
        Disabled
    }
}
