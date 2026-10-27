namespace Gebhardt.Acc.Vda5050Simulator.Runtime;

/// <summary>
/// Spawns the agvs of the persisted configurations once the fleet has started.
/// </summary>
public sealed class AgvTypeConfigurationLoader(AgvTypeConfigurationService service) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return service.SpawnPersistedAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}