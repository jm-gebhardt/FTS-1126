using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Graph;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Map;
using MassTransit;
using Microsoft.Extensions.Logging;
using Polly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation
{
    /// <summary>
    /// The SimulatedAgvProvider can create simulated AGV orchestrated by <see cref="SimulatedAgvStateMachine"/>
    /// </summary>
    public class SimulatedAgvProvider : IConsumer<MassTransitBusEvent>
    {
        private readonly SimulateOptions _simulateOptions;

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

        private readonly ILogger<SimulatedAgvProvider>? _logger;
        private readonly IRequestClient<SimulatedAgvStateMachine.CreateAgv> _client;

        /// <summary>
        /// SimulatedAgvProvider Constructor
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        /// <param name="simulateOptions"></param>
        public SimulatedAgvProvider(SimulateOptions simulateOptions, IRequestClient<SimulatedAgvStateMachine.CreateAgv> client, ILogger<SimulatedAgvProvider>? logger = null)
        {
            _logger = logger;
            _client = client;
            _simulateOptions = simulateOptions;
            _sources = simulateOptions.Sources != null ? simulateOptions.Sources.ToArray() : Array.Empty<string>();
            _destinations = simulateOptions.Destinations != null ? simulateOptions.Destinations.ToArray() : Array.Empty<string>();
        }

        /// <summary>
        /// Handle MassTransit bus events
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Task completed</returns>
        public Task Consume(ConsumeContext<MassTransitBusEvent> context)
        {
            if (context.Message.Type == MassTransitBusEvent.EventType.PostStarted)
            {
                Create();
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Create the AGVs by requesting <see cref="SimulatedAgvStateMachine.CreateAgvResponse"/> which triggers <see cref="SimulatedAgvStateMachine.CreateAgv"/>
        /// </summary>
        private void Create()
        {
            _logger?.LogInformation("Create AGV started");
            for (int agvIndex = 0; agvIndex < _simulateOptions.Agvs; agvIndex++)
            {
                var agv = GenerateAgv(agvIndex);

                var token = new CancellationTokenSource();
                token.CancelAfter(1000);
                Policy.Handle<TaskCanceledException>()
                    .RetryAsync(5, onRetry: (_, retryCount) =>
                    {
                        _logger?.LogInformation("Attempt[{RetryCount}]AGV[{AgvId}] Waiting for state machine to become ready", retryCount, agv.AgvId);
                    })
                    .ExecuteAsync(
                        async (ct) =>
                        {
                            Response<SimulatedAgvStateMachine.CreateAgvResponse> response = await _client.GetResponse<SimulatedAgvStateMachine.CreateAgvResponse>(new SimulatedAgvStateMachine.CreateAgv(
                                agv.CorrelationId,
                                agv,
                                _simulateOptions.NodeTravelTime
                            ), ct);

                            if (response.Message.CorrelationId == agv.CorrelationId)
                            {
                                _logger?.LogInformation("AGV[{AgvId}]. State machine ready. Agv created.", agv.AgvId);
                            }
                        }, token.Token);
            }
            _logger?.LogInformation("Create AGV finished");
        }

        private SimulatedAgv GenerateAgv(int agvIndex)
        {
            string agvId = $"{AgvPrefix}-{agvIndex}";
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
    }
}