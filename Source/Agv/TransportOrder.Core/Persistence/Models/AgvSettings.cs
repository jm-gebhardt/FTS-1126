using Gebhardt.StoreWare.Agv.Common.Configurations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Models
{
    /// <summary>
    /// The settings for Agv
    /// </summary>
    public class AgvSettings : ISettings<AgvSettings>
    {
        /// <summary>
        /// Timeout value [ms] to set AGV offline while it is in idle mode
        /// </summary>
        public int AgvOfflineTimeoutMsWhileIdle { get; init; } = 15000;

        /// <summary>
        /// Timeout value [ms] to set AGV offline while it is in busy mode
        /// </summary>
        public int AgvOfflineTimeoutMsWhileBusy { get; init; } = 20000;
    }
}