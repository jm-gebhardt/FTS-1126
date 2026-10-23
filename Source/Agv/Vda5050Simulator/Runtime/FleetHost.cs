using System.Collections.Concurrent;
using Gebhardt.Acc.Vda5050Simulator.Configuration;
using Gebhardt.Acc.Vda5050Simulator.Domain;
using Microsoft.Extensions.Options;

namespace Gebhardt.Acc.Vda5050Simulator.Runtime;

/// <summary>
/// Owns all AGV actors, routes commands to them and advances them with the simulation time.
/// </summary>
public sealed class FleetHost(ISimulationClock clock, IAgvPublisher publisher, IFactsheetProvider factsheetProvider, IOptions<SimulationOptions> simulationOptions, ILoggerFactory loggerFactory)
    : IHostedService
{
    private readonly ConcurrentDictionary<(string Manufacturer, string SerialNumber), AgvActor> _actors = new();
    private readonly ILogger<FleetHost> _logger = loggerFactory.CreateLogger<FleetHost>();
    private readonly TimeSpan _stateInterval = TimeSpan.FromSeconds(Math.Max(1, simulationOptions.Value.StateIntervalSeconds));
    private readonly TimeSpan _actionDuration = TimeSpan.FromSeconds(Math.Max(0.1, simulationOptions.Value.ActionDurationSeconds));
    private CancellationTokenSource? _cts;

    public IReadOnlyCollection<AgvIdentity> AgvIds => _actors.Values.Select(a => a.Id).ToList();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = new CancellationTokenSource();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(_actors.Values.Select(a => a.StopAsync()));
        _cts?.Cancel();
    }

    public bool AddAgv(AgvIdentity id, double maxSpeed = 1.0)
    {
        var model = new AgvModel(id, maxSpeed, _actionDuration);
        var actor = new AgvActor(model, clock, publisher, _stateInterval, factsheetProvider, loggerFactory.CreateLogger<AgvActor>());
        if (!_actors.TryAdd((id.Manufacturer, id.SerialNumber), actor))
        {
            return false;
        }
        actor.Start(_cts?.Token ?? CancellationToken.None);
        return true;
    }

    public async Task<bool> RemoveAgvAsync(AgvIdentity id)
    {
        if (!_actors.TryRemove((id.Manufacturer, id.SerialNumber), out var actor))
        {
            return false;
        }
        await actor.StopAsync();
        return true;
    }

    public bool Dispatch(AgvCommand command)
    {
        if (_actors.TryGetValue((command.AgvId.Manufacturer, command.AgvId.SerialNumber), out var actor))
        {
            actor.Post(command);
            return true;
        }
        _logger.LogWarning("Command {Command} for unknown AGV {AgvId} dropped", command.GetType().Name, command.AgvId);
        return false;
    }

    public async Task<CommandResult?> SendAsync(AgvCommand command, CancellationToken cancellationToken = default)
    {
        if (_actors.TryGetValue((command.AgvId.Manufacturer, command.AgvId.SerialNumber), out var actor))
        {
            return await actor.SendAsync(command).WaitAsync(cancellationToken);
        }
        _logger.LogWarning("Command {Command} for unknown AGV {AgvId} dropped", command.GetType().Name, command.AgvId);
        return null;
    }

    public bool TryGetAgv(Guid correlationId, out AgvIdentity id)
    {
        foreach (var actor in _actors.Values)
        {
            if (actor.Id.CorrelationId == correlationId)
            {
                id = actor.Id;
                return true;
            }
        }
        id = default;
        return false;
    }

    public Task AdvanceAsync(TimeSpan dt)
    {
        return Task.WhenAll(_actors.Values.Select(a => a.AdvanceAsync(dt)));
    }
}
