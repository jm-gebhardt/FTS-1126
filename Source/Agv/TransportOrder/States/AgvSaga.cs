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
    public class AgvSaga : SagaStateMachineInstance, IAgv
    {
        /// <inheritdoc cref="IAgv"/>
        public Guid CorrelationId { get; set; }

        /// <inheritdoc />
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
        /// Last Received AgvData
        /// </summary>
        public AgvData? AgvData { get; set; }

        /// <summary>
        /// Timestamp when the Agv received its data the last time
        /// </summary>
        /// <remarks>Nullable declaration required to make data model mappable according to EF Core</remarks>
        public DateTime? AgvDataReceivedLastTime { get; set; }

        /// <summary>
        /// Timestamp when the Agv received its graph the last time
        /// </summary>
        /// <remarks>Nullable declaration required to make data model mappable according to EF Core</remarks>
        public DateTime? AgvGraphReceivedLastTime { get; set; }

        /// <inheritdoc />
        public bool Alive { get; set; }

        /// <summary>
        /// Token used by the send status update schedule
        /// </summary>
        public Guid? AgvOfflineTokenId { get; set; }

        /// <inheritdoc />
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool Assignable =>
            AgvGraph is not null &&
            AgvGraph.Usability == AgvGraph.GraphUsability.Valid &&
            Alive;


        /// <summary>
        /// Default constructor required to connect.
        /// </summary>
        public AgvSaga(Guid correlationId, 
            string agvId, 
            string? currentState = null, 
            AgvStatus? agvStatus = null, 
            AgvGraph? agvGraph = null, 
            AgvData? agvData = null,
            DateTime? agvDataReceivedLastTime = null,
            DateTime? agvGraphReceivedLastTime = null)
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
            AgvData = agvData;
            AgvDataReceivedLastTime = agvDataReceivedLastTime ?? DateTime.MinValue;
            AgvGraphReceivedLastTime = agvGraphReceivedLastTime ?? DateTime.MinValue;
        }

        /// <summary>
        /// Get interval since last update of AgvDate
        /// </summary>
        /// <returns>Time span between now and last update</returns>
        public TimeSpan GetTimeSpanSinceLastUpdate_AgvData()
        {
            if (AgvDataReceivedLastTime != null)
            {
                return (TimeSpan)(DateTime.Now - AgvDataReceivedLastTime);
            }
            return TimeSpan.Zero;
        }

        /// <summary>
        /// Get interval since last update of AgvGraph
        /// </summary>
        /// <returns>Time span between now and last update</returns>
        public TimeSpan GetTimeSpanSinceLastUpdate_AgvGraph()
        {
            if (AgvGraphReceivedLastTime != null)
            {
                return (TimeSpan)(DateTime.Now - AgvGraphReceivedLastTime);
            }
            return TimeSpan.Zero;
        }
    }
}