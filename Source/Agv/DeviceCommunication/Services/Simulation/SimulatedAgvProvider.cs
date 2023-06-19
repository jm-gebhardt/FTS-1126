using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Graph;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Map;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation
{
    /// <summary>
    /// The SimulatedAgvProvider can create simulated AGV orchestrated by <see cref="SimulatedAgvStateMachine"/>
    /// </summary>
    public class SimulatedAgvProvider: BackgroundService
    {
        private readonly SimulateOptions _simulateOptions;

        private readonly string[] _sources;
        private readonly string[] _destinations;

        private const string AgvPrefix = "SimAgv-";
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

        private readonly ILogger<SimulatedAgvProvider> _logger;
        private readonly IClientFactory _clientFactory;

        /// <summary>
        /// SimulatedAgvProvider Constructor
        /// </summary>
        /// <param name="clientFactory"></param>
        /// <param name="logger"></param>
        /// <param name="simulateOptions"></param>
        public SimulatedAgvProvider(
            SimulateOptions simulateOptions, 
            IClientFactory clientFactory, 
            ILogger<SimulatedAgvProvider> logger)
        {
            _logger = logger;
            _clientFactory = clientFactory;
            _simulateOptions = simulateOptions;
            _sources = simulateOptions.Sources != null ? simulateOptions.Sources.ToArray() : Array.Empty<string>();
            _destinations = simulateOptions.Destinations != null ? simulateOptions.Destinations.ToArray() : Array.Empty<string>();
        }

        /// <summary>
        /// Create the AGVs by requesting <see cref="SimulatedAgvStateMachine.CreateAgvResponse"/> which triggers <see cref="SimulatedAgvStateMachine.CreateAgv"/>
        /// </summary>
        private async Task Create()
        {
            IRequestClient<SimulatedAgvStateMachine.CreateAgv> client = _clientFactory.CreateRequestClient<SimulatedAgvStateMachine.CreateAgv>();
            for (int agvIndex = 0; agvIndex < _simulateOptions.Agvs; agvIndex++)
            {
                var agv = GenerateAgv(agvIndex);
                Response<SimulatedAgvStateMachine.CreateAgvResponse> response = await client.GetResponse<SimulatedAgvStateMachine.CreateAgvResponse>(new SimulatedAgvStateMachine.CreateAgv(
                    agv.CorrelationId,
                    agv,
                    _simulateOptions.NodeTravelTime
                ));

                if (response.Message.CorrelationId == agv.CorrelationId)
                {
                    _logger?.LogInformation("AGV[{AgvId}]: state machine ready, agv was created", agv.AgvId);
                }
                else
                {
                    _logger?.LogError("Could not create AGV[{AgvId}]", agv.AgvId);
                }
            }
            _logger?.LogInformation("Create AGV finished");
        }

        private SimulatedAgv GenerateAgv(int agvIndex)
        {
            string agvId = $"{AgvPrefix}{agvIndex}";
            var correlationId = agvId.ToGuid();
            // Next even number from rows as number of service nodes.
            int serviceNodeCount = (_simulateOptions.GridRows / 2 + _simulateOptions.GridRows % 2) * 2;
            var serviceNodes = new AgvGraph.NodeType[serviceNodeCount];
            for (; serviceNodeCount > 0; --serviceNodeCount)
            {
                serviceNodes[serviceNodeCount - 1] = serviceNodeCount % 2 == 0 ? AgvGraph.NodeType.Park : AgvGraph.NodeType.Charge;
            }
            var grid = new GridGraphGenerator(
                _sources,
                _destinations,
                _simulateOptions.GridRows,
                serviceNodes
            ).Generate(correlationId);
            var mapGenerator = new MapGenerator(grid);
            return new SimulatedAgv(
                correlationId,
                agvId,
                "Initial",
                grid,
                GenerateData(correlationId),
                mapGenerator.Generate()
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

        /// <summary>
        /// Creates Simulated Agvs and exits
        /// </summary>
        /// <param name="stoppingToken"></param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SimulatedAgvProvider started");
            await Create();
        }
    }
}