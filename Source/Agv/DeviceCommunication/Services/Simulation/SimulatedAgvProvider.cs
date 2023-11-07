using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Graph;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Map;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;

/// <summary>
/// The SimulatedAgvProvider can create simulated AGV orchestrated by <see cref="SimulatedAgvStateMachine"/>
/// </summary>
public class SimulatedAgvProvider : BackgroundService
{
    private readonly SimulateOptions _simulateOptions;

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

    private readonly ILogger<SimulatedAgvProvider> _logger;
    private readonly IClientFactory _clientFactory;
    private readonly IGraphGenerator _graphGenerator;

    /// <summary>
    /// SimulatedAgvProvider Constructor
    /// </summary>
    /// <param name="clientFactory"></param>
    /// <param name="graphGenerator"></param>
    /// <param name="logger"></param>
    /// <param name="simulateOptions"></param>
    public SimulatedAgvProvider(
        SimulateOptions simulateOptions,
        IClientFactory clientFactory,
        IGraphGenerator graphGenerator,
        ILogger<SimulatedAgvProvider> logger)
    {
        _logger = logger;
        _graphGenerator = graphGenerator;
        _clientFactory = clientFactory;
        _simulateOptions = simulateOptions;
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
        string agvId = $"{AgvPrefix}-{agvIndex}";
        var correlationId = agvId.ToGuid();
        var grid = _graphGenerator.Generate(correlationId);
        var mapGenerator = new MapGenerator(grid);
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
            AgvData: GenerateData(correlationId),
            AgvMap: mapGenerator.Generate()
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