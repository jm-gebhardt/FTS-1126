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
        public float OccupiedDistanceThreshold { get; init; } = 1.0f;
    }
}