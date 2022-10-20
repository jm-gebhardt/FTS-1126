using Gebhardt.StoreWare.Agv.TransportOrder.Common.Models;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Common.Services
{
    /// <summary>
    /// Define interface to access AGV position registry
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public interface IAgvPositionRegistry
    {
        /// <summary>
        /// Update position of an AGV
        /// </summary>
        /// <param name="agvPositionStatus">Position message from the AGV</param>
        public void UpdatePosition(AgvPositionStatus agvPositionStatus);

        /// <summary>
        /// Get position of AGV
        /// </summary>
        /// <param name="correlationId">Correlation ID of the AGV</param>
        /// <returns>Returns the position of the requested AGV of null when no AGV with the given correlationId was found</returns>
        public AgvPositionStatus? GetPosition(Guid correlationId);

        /// <summary>
        /// Returns a list position for all AGVs
        /// </summary>
        /// <returns>Returns a list position for all AGVs</returns>
        public List<AgvPositionStatus> GetAllPositions();
    }
}
