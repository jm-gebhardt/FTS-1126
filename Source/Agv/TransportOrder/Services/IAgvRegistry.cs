
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
        /// Get list of idle AGVs
        /// </summary>
        public IEnumerable<Common.Messages.AgvManagement.Agv> GetIdleAgvs();

    }
}