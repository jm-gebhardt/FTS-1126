using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.States
{
    /// <summary>
    /// Agv Saga for simulated AGVs
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class SimulatedAgvSaga : SagaStateMachineInstance, IAgv
    {
        /// <summary>
        /// Correlation ID
        /// </summary>
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public string AgvId { get; }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public string CurrentState { get; set; }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        // ReSharper disable once ReturnTypeCanBeNotNullable
        public AgvStatus? AgvStatus => new(
            CorrelationId,
            OrderDetails?.OrderId,
            new [] {
                nameof(SimulatedAgvStateMachine.BusyMovingToSource), 
                nameof(SimulatedAgvStateMachine.BusyMovingToDestination)
            }.Any(c => c == CurrentState),
            false,
            80,
            false,
            false,
            LastNodeId
        );

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public AgvGraph? AgvGraph { get; }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public AgvData? AgvData { get; }
    
        /// <summary>
        /// The current order being processed, null if currently no order is being processed
        /// </summary>
        public OrderDetails? OrderDetails { get; set; }

        /// <summary>
        /// Token used by the send status update schedule
        /// </summary>
        public Guid? SendStatusUpdateTokenId { get; set; }
        
        /// <summary>
        /// Token used for by pick order schedule
        /// </summary>
        public Guid? PickOrderTokenId { get; set; }
    
        /// <summary>
        /// Token used for by finish order schedule
        /// </summary>
        public Guid? FinishOrderTokenId { get; set; }
    
        /// <summary>
        /// List of node ids that define the route
        /// </summary>
        public string[] Nodes { get; set; }
        
        /// <summary>
        /// Time it takes to travel from one node to another in seconds
        /// </summary>
        public double NodeTravelTime = 5;

        /// <summary>
        /// The last visited Node of the AGV
        /// </summary>
        public string? LastNodeId => OrderProgress != null
            ? Nodes[(int) (Nodes.Length * OrderProgress)] 
            : Nodes.LastOrDefault();

        /// <summary>
        /// Order Progress as percentage
        /// </summary>
        public double? OrderProgress => TimeToFinish != null && OrderDuration != null 
            ? 1 - TimeToFinish.Value.TotalSeconds / OrderDuration.Value.TotalSeconds 
            : null;
        

        /// <summary>
        /// Time the order takes to complete
        /// </summary>
        public TimeSpan? OrderDuration => TimeSpan.FromSeconds(Nodes.Length * NodeTravelTime);

        /// <summary>
        /// Time the order processing started
        /// </summary>
        public DateTime? OrderStartDate { get; set; }

        /// <summary>
        /// Time the order will be finished
        /// This value is calculated base on OrderStartDate and the number of nodes
        /// </summary>
        public DateTime? OrderFinishDate => OrderStartDate != null ? OrderStartDate + OrderDuration: null;

        /// <summary>
        /// Time until the order is finished.
        /// This values is calculated based on the current time and OrderFinishDate
        /// </summary>
        public TimeSpan? TimeToFinish => OrderFinishDate != null ? OrderFinishDate - DateTime.Now : null;
        
        /// <summary>
        /// Time until the order is finished.
        /// This values is calculated based on the current time and OrderFinishDate
        /// </summary>
        public TimeSpan? TimeToPick => OrderFinishDate != null ? OrderPickDate - DateTime.Now : null;

        
        /// <summary>
        /// Time the order will be picked up
        /// </summary>
        public DateTime? OrderPickDate => OrderFinishDate != null && OrderDetails != null 
            ? OrderStartDate + TimeSpan.FromSeconds(Array.IndexOf(Nodes, OrderDetails!.Source) * NodeTravelTime) 
            : null;

        /// <summary>
        /// SimulatedAgvSaga Constructor
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="agvId"></param>
        /// <param name="nodes"></param>
        /// <param name="currentState"></param>
        /// <param name="agvGraph"></param>
        /// <param name="agvData"></param>
        public SimulatedAgvSaga(
            Guid correlationId, 
            string agvId, 
            string[]? nodes = null, 
            string? currentState = null,
            AgvGraph? agvGraph = null, 
            AgvData? agvData = null)
        {
            if (correlationId == Guid.Empty)
            {
                throw new InvalidOperationException($"{this.GetType()} requires correlation ID.");
            }
            CorrelationId = correlationId;
            AgvId = agvId;
            Nodes = nodes ?? Array.Empty<string>();
            CurrentState = currentState ?? "";
            AgvGraph = agvGraph;
            AgvData = agvData;
        }

        /// <summary>
        /// SimulatedAgvSa Constructor
        /// </summary>
        /// <param name="agv"></param>
        public SimulatedAgvSaga(IAgv agv)
        {
            CorrelationId = agv.CorrelationId;
            AgvId = agv.AgvId;
            AgvGraph = agv.AgvGraph;
            AgvData = agv.AgvData;
            CurrentState = agv.CurrentState;
            Nodes = Array.Empty<string>();
        }

        /// <summary>
        /// Reset Order
        /// </summary>
        public void ResetOrder()
        {
            OrderDetails = null;
            OrderStartDate = null;
        }
    }
}