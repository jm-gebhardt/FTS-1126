using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
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
        /// <param name="agvPositionReceived">Position message from the AGV</param>
        public void UpdatePosition(AgvPositionReceived agvPositionReceived);

        /// <summary>
        /// Get position of AGV
        /// </summary>
        /// <param name="correlationId">Correlation ID of the AGV</param>
        /// <returns>Returns the position of the requested AGV of null when no AGV with the given correlationId was found</returns>
        public AgvPositionReceived? GetPosition(Guid correlationId);
    }
}
