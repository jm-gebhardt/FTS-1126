using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Graph;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Map;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation
{
    /// <summary>
    /// The SimulatedAgvProvider can create simulated AGV orchestrated by <see cref="SimulatedAgvStateMachine"/>
    /// </summary>
    public class SimulatedAgvProvider
    {
        private readonly SimulateOptions _simulateOptions;
        private readonly IBus _bus;

        private readonly string[] _sources;
        private readonly string[] _destinations;

        private const string AgvPrefix = "SimAgv";
        private const string Model = "R2-D2";
        private const string Type = "Simulated AGV";
        private const double Width = 1;
        private const double Length = 1;
        private const double BatteryCapacity = 100;
        private const double Acceleration = 1;
        private const double MaxVelocityStraight = 1;
        private const double MaxVelocityTurn = 1;
        private const double MaxVelocityTurnOnSpot = 1;
        private const long Payload = 50;
        private static readonly AgvData.Sensor[] _sensors = Array.Empty<AgvData.Sensor>();

        private record Agv(Guid CorrelationId, string AgvId, string CurrentState, AgvStatus? AgvStatus, AgvGraph? AgvGraph, AgvData? AgvData) : IAgv;


        /// <summary>
        /// SimulatedAgvProvider Constructor
        /// </summary>
        /// <param name="simulateOptions"></param>
        /// <param name="bus"></param>
        public SimulatedAgvProvider(SimulateOptions simulateOptions, IBus bus)
        {
            _simulateOptions = simulateOptions;
            _sources = simulateOptions.Sources != null ? simulateOptions.Sources.ToArray() : Array.Empty<string>();
            _destinations = simulateOptions.Destinations != null ? simulateOptions.Destinations.ToArray() : Array.Empty<string>();
            _bus = bus;
        }


        /// <summary>
        /// Create the AGVs by publishing <see cref="SimulatedAgvStateMachine.CreateAgv"/>
        /// </summary>
        public void Create()
        {
            for (int agvIndex = 0; agvIndex < _simulateOptions.Agvs; agvIndex++)
            {
                var agv = GenerateAgv(agvIndex);
                var map = new MapGenerator(agv.AgvGraph!);
                _bus.Publish(new SimulatedAgvStateMachine.CreateAgv(
                    agv.CorrelationId,
                    agv,
                    map.Generate()
                ));
            }
        }

        private Agv GenerateAgv(int agvIndex)
        {
            string agvId = $"{AgvPrefix}-{agvIndex}";
            var correlationId = agvId.ToGuid();
            var grid = new GridGraphGenerator(
                _sources,
                _destinations,
                serviceNodes: new[] {AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park, AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park}
            ).Generate(correlationId);
            return new Agv(
                correlationId,
                agvId,
                "Initial",
                null,
                grid,
                GenerateData(correlationId)
            );
        }

        private AgvData GenerateData(Guid correlationId)
        {
            return new AgvData(
                correlationId,
                Model,
                Type,
                null,
                BatteryCapacity,
                Acceleration,
                MaxVelocityStraight,
                MaxVelocityTurn,
                MaxVelocityTurnOnSpot,
                Width,
                Length,
                Payload,
                _sensors
            );
        }

    }
}