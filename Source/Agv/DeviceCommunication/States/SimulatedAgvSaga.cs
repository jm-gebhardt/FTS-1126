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

        /// <inheritdoc cref="IAgv" />
        public string AgvId { get; }

        /// <inheritdoc cref="IAgv" />
        public string CurrentState { get; set; }

        /// <inheritdoc cref="IAgv" />
        public bool Alive => true;

        /// <inheritdoc cref="IAgv" />
        public bool Assignable => true;

        /// <inheritdoc cref="IAgv" />
        // ReSharper disable once ReturnTypeCanBeNotNullable
        public AgvStatus? AgvStatus => new(
            CorrelationId,
            OrderDetails?.OrderId,
            new[] {
                nameof(SimulatedAgvStateMachine.BusyMovingToSource),
                nameof(SimulatedAgvStateMachine.BusyMovingToDestination)
            }.Any(c => c == CurrentState),
            false,
            80,
            false,
            false,
            LastNodeId
        );

        /// <inheritdoc cref="IAgv" />
        public AgvGraph? AgvGraph { get; }

        /// <inheritdoc cref="IAgv" />
        public AgvData? AgvData { get; }

        /// <summary>
        /// Returns the map according to <see cref="AgvGraph"/>.
        /// </summary>
        public AgvMap? AgvMap { get; }

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
        /// Token used for signal position scheduling
        /// </summary>
        public Guid? SignalPositionTokenId { get; set; }

        /// <summary>
        /// List of node ids that define the route
        /// </summary>
        public string[] Nodes { get; set; }

        /// <summary>
        /// Time it takes to travel from one node to another in milliseconds
        /// </summary>
        public int NodeTravelTime { get; set; }

        /// <summary>
        /// Returns the node last visited by the AGV
        /// </summary>
        public string? LastNodeId
        {
            get
            {
                double? orderProgress = OrderProgress; // Access time dependent value just once
                if (orderProgress.HasValue)
                {
                    int lastNodeIndex = (int)(Nodes.Length * orderProgress.Value);
                    if (lastNodeIndex < Nodes.Length)
                    {
                        return Nodes[lastNodeIndex];
                    }
                }
                return Nodes.LastOrDefault();
            }
        }

        /// <summary>
        /// Next node AGV will arrive at.
        /// On destination arrival this is the same as <see cref="LastNodeId"/>.
        /// </summary>
        public string? NextNodeId
        {
            get
            {
                string? lastNodeId = LastNodeId; // Access time dependent value just once
                if (lastNodeId != null && lastNodeId != Nodes.Last())
                {
                    return Nodes[Array.IndexOf(Nodes, lastNodeId) + 1];
                }
                return lastNodeId;
            }
        }

        /// <summary>
        /// Return the current position based on the time passed traveling.
        /// </summary>
        public AgvPosition? Position
        {
            get
            {
                double? orderProgress = OrderProgress; // Access time dependent value just once
                if (AgvGraph != null && orderProgress.HasValue)
                {
                    int lastNodeIndex = (int)(Nodes.Length * orderProgress.Value);
                    if (lastNodeIndex >= Nodes.Length)
                    {
                        // This happens when accessing position when or after order has finished
                        lastNodeIndex = Nodes.Length - 1;
                    }
                    var lastNode = AgvGraph!.Nodes.First(x => x.Id == Nodes[lastNodeIndex]);
                    if (lastNodeIndex < Nodes.Length - 1)
                    {
                        double partialProgress = (orderProgress.Value - lastNodeIndex / (double)Nodes.Length) * Nodes.Length;
                        var nextNode = AgvGraph!.Nodes.First(x => x.Id == Nodes[lastNodeIndex + 1]);
                        var vectorLast = new System.Numerics.Vector3(lastNode.X, lastNode.Y, lastNode.Z);
                        var vectorNext = new System.Numerics.Vector3(nextNode.X, nextNode.Y, nextNode.Z);
                        float theta = (float)(Math.Atan2(vectorNext.Y - vectorLast.Y, vectorNext.X - vectorLast.X) + Math.PI / 2);
                        var vectorEdge = vectorNext - vectorLast;
                        vectorEdge *= (float)partialProgress;
                        vectorEdge = vectorLast + vectorEdge;
                        return new AgvPosition(true, vectorEdge.X, vectorEdge.Y, vectorEdge.Z, theta, 0f);
                    }
                    return new AgvPosition(true, lastNode.X, lastNode.Y, lastNode.Z, 0f, 0f);
                }
                return null;
            }
        }

        /// <summary>
        /// Order Progress as percentage [0..1]
        /// </summary>
        public double? OrderProgress => TimeToFinish != null && OrderDuration != null
            ? 1 - TimeToFinish.Value.TotalSeconds / OrderDuration.Value.TotalSeconds
            : null;

        /// <summary>
        /// Time the order takes to complete
        /// </summary>
        public TimeSpan? OrderDuration => TimeSpan.FromMilliseconds(Nodes.Length * NodeTravelTime);

        /// <summary>
        /// Time the order processing started
        /// </summary>
        public DateTime? OrderStartDate { get; set; }

        /// <summary>
        /// Time the order will be finished
        /// This value is calculated base on OrderStartDate and the number of nodes
        /// </summary>
        public DateTime? OrderFinishDate => OrderStartDate != null ? OrderStartDate + OrderDuration : null;

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
            ? OrderStartDate + TimeSpan.FromMilliseconds(Array.IndexOf(Nodes, OrderDetails!.Source) * NodeTravelTime)
            : null;

        /// <summary>
        /// SimulatedAgvSaga Constructor
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="agvId"></param>
        /// <param name="nodeTravelTime"></param>
        /// <param name="nodes"></param>
        /// <param name="currentState"></param>
        /// <param name="agvGraph"></param>
        /// <param name="agvData"></param>
        /// <param name="agvMap"></param>
        public SimulatedAgvSaga(
            Guid correlationId,
            string agvId,
            int nodeTravelTime,
            string[]? nodes = null,
            string? currentState = null,
            AgvGraph? agvGraph = null,
            AgvData? agvData = null,
            AgvMap? agvMap = null)
        {
            if (correlationId == Guid.Empty)
            {
                throw new InvalidOperationException($"{this.GetType()} requires correlation ID.");
            }
            CorrelationId = correlationId;
            AgvId = agvId;
            NodeTravelTime = nodeTravelTime;
            Nodes = nodes ?? Array.Empty<string>();
            CurrentState = currentState ?? "";
            AgvGraph = agvGraph;
            AgvData = agvData;
            AgvMap = agvMap;
        }

        /// <summary>
        /// SimulatedAgvSa Constructor
        /// </summary>
        /// <param name="agv"></param>
        /// <param name="nodeTravelTime"></param>
        public SimulatedAgvSaga(SimulatedAgv agv, int nodeTravelTime)
        {
            CorrelationId = agv.CorrelationId;
            AgvId = agv.AgvId;
            AgvGraph = agv.AgvGraph;
            AgvData = agv.AgvData;
            CurrentState = agv.CurrentState;
            Nodes = Array.Empty<string>();
            AgvMap = agv.AgvMap;
            NodeTravelTime = nodeTravelTime;
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
