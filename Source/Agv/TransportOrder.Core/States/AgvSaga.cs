using Gebhardt.StoreWare.Agv.Common.Configurations;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Models;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.States
{
    /// <summary>
    /// AGV Saga used by <see cref="Services.AgvStateMachine"/>.
    /// This stores the current AGV state
    /// </summary>
    [PublicAPI]
    public class AgvSaga : SagaStateMachineInstance, IAgv, ISagaDefinedSetting<AgvSagaSettings, AgvSettings>
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
        /// UTC timestamp when AGV data was requested
        /// </summary>
        /// <remarks>Nullable declaration required to make data model mappable according to EF Core</remarks>
        public DateTime? AgvDataRequestedLastTime { get; set; }

        /// <summary>
        /// UTC timestamp when AGV graph was requested
        /// </summary>
        /// <remarks>Nullable declaration required to make data model mappable according to EF Core</remarks>
        public DateTime? AgvGraphRequestedLastTime { get; set; }

        /// <inheritdoc />
        public bool Alive { get; set; }

        /// <summary>
        /// Token used by the send status update schedule
        /// </summary>
        public Guid? AgvOfflineTokenId { get; set; }

        /// <summary>
        /// Agv Saga Settings with nullable properties
        /// </summary>
        public AgvSagaSettings? Settings { get; set; }

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
            AgvData? agvData = null)
        {
            if (correlationId == Guid.Empty)
            {
                throw new InvalidOperationException($"{this.GetType()} requires correlation ID.");
            }
            CorrelationId = correlationId;
            AgvId = agvId;
            CurrentState = currentState ?? default!;
            AgvStatus = agvStatus;
            AgvGraph = agvGraph;
            AgvData = agvData;
        }

        /// <summary>
        /// Get interval since last update of AgvDate
        /// </summary>
        /// <returns>Time span between now and last update</returns>
        public TimeSpan GetTimeSpanSinceLastRequest_AgvData()
        {
            if (AgvDataRequestedLastTime != null)
            {
                return (TimeSpan)(DateTime.UtcNow - AgvDataRequestedLastTime);
            }
            return TimeSpan.MaxValue;
        }

        /// <summary>
        /// Get interval since last update of AgvGraph
        /// </summary>
        /// <returns>Time span between now and last update</returns>
        public TimeSpan GetTimeSpanSinceLastRequest_AgvGraph()
        {
            if (AgvGraphRequestedLastTime != null)
            {
                return (TimeSpan)(DateTime.UtcNow - AgvGraphRequestedLastTime);
            }
            return TimeSpan.MaxValue;
        }
    }
}