using System.Collections.Concurrent;
using Gebhardt.Acc.Vda5050Simulator.Configuration;
using Gebhardt.Acc.Vda5050Simulator.Domain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gebhardt.Acc.Vda5050Simulator.Runtime;

/// <summary>
/// Owns all AGV actors, routes commands to them and advances them with the simulation time.
/// </summary>
public sealed class FleetHost : IHostedService
{
    private readonly ConcurrentDictionary<AgvId, AgvActor> _actors = new();
    private readonly ISimulationClock _clock;
    private readonly IAgvPublisher _publisher;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<FleetHost> _logger;
    private readonly IReadOnlyList<AgvOptions> _agvOptions;
    private readonly TimeSpan _stateInterval;
    private readonly TimeSpan _actionDuration;
    private CancellationTokenSource? _cts;

    public FleetHost(
        ISimulationClock clock,
        IAgvPublisher publisher,
        IOptions<List<AgvOptions>> agvOptions,
        IOptions<SimulationOptions> simulationOptions,
        ILoggerFactory loggerFactory)
    {
        _clock = clock;
        _publisher = publisher;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<FleetHost>();
        _agvOptions = agvOptions.Value;
        _stateInterval = TimeSpan.FromSeconds(Math.Max(1, simulationOptions.Value.StateIntervalSeconds));
        _actionDuration = TimeSpan.FromSeconds(Math.Max(0.1, simulationOptions.Value.ActionDurationSeconds));
    }

    public IReadOnlyCollection<AgvId> AgvIds => _actors.Keys.ToList();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = new CancellationTokenSource();
        foreach (var agv in _agvOptions)
        {
            AddAgv(new AgvId(agv.Manufacturer, agv.SerialNumber), agv.MaxSpeed);
        }
        _logger.LogInformation("Fleet started with {Count} AGVs", _actors.Count);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(_actors.Values.Select(a => a.StopAsync()));
        _cts?.Cancel();
    }

    public bool AddAgv(AgvId id, double maxSpeed = 1.0)
    {
        var model = new AgvModel(id, maxSpeed, _actionDuration);
        var actor = new AgvActor(model, _clock, _publisher, _stateInterval, _loggerFactory.CreateLogger<AgvActor>());
        if (!_actors.TryAdd(id, actor))
        {
            return false;
        }
        actor.Start(_cts?.Token ?? CancellationToken.None);
        return true;
    }

    public bool Dispatch(AgvCommand command)
    {
        if (_actors.TryGetValue(command.AgvId, out var actor))
        {
            actor.Post(command);
            return true;
        }
        _logger.LogWarning("Command {Command} for unknown AGV {AgvId} dropped", command.GetType().Name, command.AgvId);
        return false;
    }

    public Task AdvanceAsync(TimeSpan dt)
    {
        return Task.WhenAll(_actors.Values.Select(a => a.AdvanceAsync(dt)));
    }
}
