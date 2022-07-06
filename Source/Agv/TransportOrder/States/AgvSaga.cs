using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.States
{

    /// <summary>
    /// AGV Saga used by AgvStateMachine
    /// This stores the current AGV state
    /// </summary>
    [PublicAPI]
    public class AgvSaga :
        SagaStateMachineInstance, IAgv
    {
        
        /// <summary>
        /// CorrelationId
        /// <see href="https://masstransit-project.com//usage/correlation.html" />
        /// </summary>
        public Guid CorrelationId { get; set; }
        
        /// <summary>
        /// Agv Serial Number
        /// </summary>
        public string AgvId { get; set; }

        /// <summary>
        /// Current State
        /// </summary>
        public string CurrentState { get; set; }
        
        /// <summary>
        /// Last Received AgvStatus
        /// </summary>
        public AgvStatus? AgvStatus { get; set; }
        
        /// <summary>
        /// Last Received AgvGraph
        /// </summary>
        public AgvGraph? AgvGraph { get; set; }
        
        
        /// <summary>
        /// Default constructor required to connect.
        /// </summary>
        public AgvSaga(Guid correlationId, string agvId, string? currentState = null, AgvStatus? agvStatus = null, AgvGraph? agvGraph = null)
        {
            if (correlationId == Guid.Empty)
            {
                throw new InvalidOperationException($"{this.GetType()} requires correlation ID.");
            }
            CorrelationId = correlationId;
            AgvStatus = agvStatus;
            AgvGraph = agvGraph;
            AgvId = agvId;
            CurrentState = currentState ?? default!;
        }
    }
}