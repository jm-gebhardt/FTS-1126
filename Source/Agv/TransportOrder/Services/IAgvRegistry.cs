using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{
    /// <summary>
    /// Interaface for accessing the AGV registry
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public interface IAgvRegistry
    {
        /// <summary>
        /// Get count of all AGVs in the registry
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Get count of idle AGVs in the registry
        /// </summary>
        public int CountIdle { get; }

        /// <summary>
        /// Get list of all AGVs
        /// </summary>
        public IEnumerable<IAgv> GetAllAgvs();

        /// <summary>
        /// Get list of idle AGVs
        /// </summary>
        public IEnumerable<IAgv> GetIdleAgvs();

        /// <summary>
        /// Get specific AGV
        /// </summary>
        /// <returns>IAgv object or null if no AGV with the given correlationId was found</returns>
        public IAgv? GetAgv(Guid correlationId);
    }
}
