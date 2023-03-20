using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services
{
    /// <summary>
    /// Registry of AGV's important states, such as status, agv action and agv graph, to retrieve or modify last agvStatus and agvMap of Agv
    /// </summary>
    public interface IAgvRegistry
    {
        /// <summary>
        /// Retrieve last state information of Agv for given correlationId
        /// </summary>
        /// <param name="correlationId">The CorrelationId of the Agv</param>
        /// <returns>Null if there is no information yet for the given Agv</returns>
        public AgvInformation? GetAgvInformation(Guid correlationId);

        /// <summary>
        /// Updates the registry with agv status, agv graph and agv action
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="agvInformation"></param>
        public void AddOrUpdate(Guid correlationId, AgvInformation agvInformation);

        /// <summary>
        /// Updates the registry record's agv graph
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="agvGraph"></param>
        public void AddOrUpdateAgvGraph(Guid correlationId, AgvGraph agvGraph);


        /// <summary>
        /// Update the registry record's AgvStatusReceived
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="agvStatusReceived"></param>
        public void AddOrUpdateAgvStatus(Guid correlationId, AgvStatusReceived agvStatusReceived);

        /// <summary>
        /// Update the registry record's agv action
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="agvAction"></param>
        public void AddOrUpdateAgvAction(Guid correlationId, AgvAction agvAction);
    }
}
