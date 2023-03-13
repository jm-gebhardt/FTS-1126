using Gebhardt.StoreWare.Agv.Common.Configurations.Models;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Models
{
    /// <summary>
    /// The settings for Agv
    /// </summary>
    public record AgvSettings : SettingsBase<AgvSettings>
    {
        /// <summary>
        /// Timeout value [ms] to set AGV offline while it is in idle mode
        /// </summary>
        public int AgvOfflineTimeoutMsWhileIdle { get; init; } = 15000;

        /// <summary>
        /// Timeout value [ms] to set AGV offline while it is in busy mode
        /// </summary>
        public int AgvOfflineTimeoutMsWhileBusy { get; init; } = 20000;

        /// <summary>
        /// Min. distance between an AGV and a node for a node to be considered 'Occupied'
        /// </summary>
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public float OccupiedDistanceThreshold { get; init; } = 1.0f;

        /// <summary>Critical battery level</summary>
        public int BatteryLevelCritical { get; init; } = 10;

        /// <summary>Low battery level</summary>
        public int BatteryLevelLow { get; init; } = 40;

        /// <summary>Good battery level</summary>
        public int BatteryLevelGood { get; init; } = 80;

        /// <summary>Full battery level</summary>
        public int BatteryLevelFull { get; init; } = 100;
    }
}