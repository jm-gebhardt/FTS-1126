using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

// ReSharper disable InvalidUsage
namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation
{
    /// <summary>
    /// Represents an AGV for simulation.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record SimulatedAgv : IAgv
    {
        /// <inheritdoc />
        public Guid CorrelationId { get; }

        /// <inheritdoc />
        public string AgvId { get; }

        /// <inheritdoc />
        public string CurrentState { get; }

        /// <inheritdoc />
        public bool IsOnline { get; }

        /// <inheritdoc />
        public bool IsLocked { get; set; }

        /// <inheritdoc />
        public bool HasError { get; }

        /// <inheritdoc />
        public DateTime TimeOfStateTransition { get; set; }

        /// <inheritdoc />
        public DateTime TimeOfLockStateChanged { get; set; }

        /// <inheritdoc />
        public AgvStatus? AgvStatus { get; }

        /// <summary>
        /// We still save the graph in the simulated Agv for convenience
        /// </summary>
        public AgvGraph? AgvGraph { get; }

        /// <inheritdoc cref="IAgv" />
        public AgvData? AgvData { get; }

        /// <summary>
        /// AGV map
        /// </summary>
        public AgvMap? AgvMap { get; }

        /// <summary>
        /// Build simulated AGV
        /// </summary>
        /// <param name="correlationId">Correlation ID of the AGV</param>
        /// <param name="agvId">AGV ID</param>
        /// <param name="currentState">Current AGV status</param>
        /// <param name="agvGraph">AGV graph</param>
        /// <param name="agvData">AGV data</param>
        /// <param name="agvMap">AGV map</param>
        public SimulatedAgv(Guid correlationId,
            string agvId,
            string currentState,
            AgvGraph? agvGraph,
            AgvData? agvData,
            AgvMap? agvMap)
        {
            CorrelationId = correlationId;
            AgvId = agvId;
            CurrentState = currentState;
            AgvGraph = agvGraph;
            AgvData = agvData;
            AgvMap = agvMap;
        }
    }
}