using Gebhardt.StoreWare.Agv.Common.Messages.Graphs.Events;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Data;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Graphs;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;

/// <summary>
/// The SimulatedAgvProvider can create simulated AGV orchestrated by <see cref="SimulatedAgvStateMachine"/>
/// </summary>
public class SimulatedAgvProvider : BackgroundService, IConsumer<GraphMissing>
{
    private readonly SimulateOptions _simulateOptions;

    private const string AgvPrefix = "SimAgv";

    private readonly ILogger<SimulatedAgvProvider> _logger;
    private readonly IClientFactory _clientFactory;
    private readonly IGraphGenerator _graphGenerator;
    private readonly IBus _bus;
    private readonly IAgvDataGenerator _agvDataGenerator;

    private readonly RequestTimeout _requestTimeOut = RequestTimeout.After(m: 1);
    private readonly Common.Models.Graphs.Graph _graph;
    private readonly List<Common.Models.Graphs.Map> _maps;
    private const int ExecutionDelayMs = 1000;

    /// <summary>
    /// SimulatedAgvProvider Constructor
    /// </summary>
    /// <param name="clientFactory"></param>
    /// <param name="graphGenerator"></param>
    /// <param name="bus"></param>
    /// <param name="logger"></param>
    /// <param name="simulateOptions"></param>
    public SimulatedAgvProvider(
        SimulateOptions simulateOptions,
        IClientFactory clientFactory,
        IGraphGenerator graphGenerator,
        IBus bus,
        ILogger<SimulatedAgvProvider> logger)
    {
        _logger = logger;
        _graphGenerator = graphGenerator;
        _bus = bus;
        _clientFactory = clientFactory;
        _simulateOptions = simulateOptions;
        _agvDataGenerator = new KarisAgvDataGenerator();
        _graph = _graphGenerator.GenerateGraph();
        _maps = _graphGenerator.GenerateMaps().ToList();
    }

    /// <summary>
    /// Create the AGVs by requesting <see cref="SimulatedAgvStateMachine.CreateAgvResponse"/> which triggers <see cref="SimulatedAgvStateMachine.CreateAgv"/>
    /// </summary>
    private async Task<bool> CreateAgvAsync()
    {
        IRequestClient<SimulatedAgvStateMachine.CreateAgv> client = _clientFactory.CreateRequestClient<SimulatedAgvStateMachine.CreateAgv>(_requestTimeOut);
        for (int agvIndex = 0; agvIndex < _simulateOptions.Agvs; agvIndex++)
        {
            var agv = GenerateAgv(agvIndex);

            Response<SimulatedAgvStateMachine.CreateAgvResponse> response = await client.GetResponse<SimulatedAgvStateMachine.CreateAgvResponse>(new SimulatedAgvStateMachine.CreateAgv(
                agv.CorrelationId,
                agv,
                _simulateOptions.SimulationSpeed
            ), timeout: _requestTimeOut);

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
        return true;
    }

    private SimulatedAgv GenerateAgv(int agvIndex)
    {
        string agvId = $"{AgvPrefix}-{agvIndex}";
        var correlationId = agvId.ToGuid();
        var grid = _graphGenerator.Generate(correlationId);

        return new SimulatedAgv(
            correlationId,
            agvId,
            "Initial",
            true,
            false,
            false,
            true,
            DateTime.MinValue,
            DateTime.MinValue,
            AgvGraph: grid,
            AgvData: _agvDataGenerator.Generate(agvIndex, correlationId)
        );
    }

    private async Task PublishGraphAndMapsAsync()
    {
        await _bus.Publish(new GraphChanged(Guid.NewGuid(), _graph, _maps));
    }

    /// <summary>
    /// Creates Simulated Agvs and exits
    /// </summary>
    /// <param name="stoppingToken"></param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SimulatedAgvProvider started");
        // (global) graph must be existing before creating AGVs
        await PublishGraphAndMapsAsync();

        bool agvsCreated = false;
        while (!agvsCreated && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                agvsCreated = await CreateAgvAsync();
            }
            catch (RequestTimeoutException)
            {
                _logger.LogWarning("Creating agvs failed, trying again in next cycle.");
                await Task.Delay(ExecutionDelayMs, stoppingToken).WaitAsync(stoppingToken);
            }
        }
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<GraphMissing> context) => PublishGraphAndMapsAsync();
}