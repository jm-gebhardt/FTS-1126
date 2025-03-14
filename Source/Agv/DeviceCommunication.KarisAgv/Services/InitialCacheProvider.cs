using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;
using Microsoft.Extensions.Hosting;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;

/// <summary>
/// Provides caches during service initialization.
/// </summary>
public class InitialCacheProvider(IAgvRepository agvRepository, IGraphRepository graphRepository) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.WhenAll(agvRepository.LoadCache(), graphRepository.LoadCache());
    }
}