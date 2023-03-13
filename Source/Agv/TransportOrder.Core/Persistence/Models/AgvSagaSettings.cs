using Gebhardt.StoreWare.Agv.Common.Configurations;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Models
{
    /// <summary>
    /// The settings for Agv which are serialized in the <see ref="AgvSaga"/>
    /// </summary>
    public record AgvSagaSettings : ISagaSettings<AgvSettings>
    {
        /// <summary>
        /// Timeout value [ms] to set AGV offline while it is in idle mode
        /// </summary>
        [UsedImplicitly]
        public int? AgvOfflineTimeoutMsWhileIdle { get; init; }

        /// <summary>
        /// Timeout value [ms] to set AGV offline while it is in busy mode
        /// </summary>
        [UsedImplicitly]
        public int? AgvOfflineTimeoutMsWhileBusy { get; init; }

        /// <summary>
        /// Min. distance between an AGV and a node for a node to be considered 'Occupied'
        /// </summary>
        [UsedImplicitly]
        public float OccupiedDistanceThreshold { get; init; } = 1.0f;

        /// <summary>Critical battery level</summary>
        [UsedImplicitly]
        public int? BatteryLevelCritical { get; init; }

        /// <summary>Low battery level</summary>
        [UsedImplicitly]
        public int? BatteryLevelLow { get; init; }

        /// <summary>Good battery level</summary>
        [UsedImplicitly]
        public int? BatteryLevelGood { get; init; }

        /// <summary>Full battery level</summary>
        [UsedImplicitly]
        public int? BatteryLevelFull { get; init; }
    }
}