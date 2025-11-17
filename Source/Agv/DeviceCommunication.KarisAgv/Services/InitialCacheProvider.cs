using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;

/// <summary>
/// Provides caches during service initialization.
/// </summary>
public class InitialCacheProvider(IServiceProvider serviceProvider) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();

        var agvRepository = scope.ServiceProvider.GetRequiredService<IAgvRepository>();
        var graphRepository = scope.ServiceProvider.GetRequiredService<IGraphRepository>();

        await Task.WhenAll(agvRepository.LoadCache(), graphRepository.LoadCache());
    }
}