using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;

namespace Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;

/// <summary>
/// Provides a service to initialize the in memory caches.
/// </summary>
[UsedImplicitly]
public class InMemoryCacheInitializer(ITriggerRepository triggerRepository, ICargoRepository cargoRepository) : BackgroundService
{
    /// <inheritdoc cref="BackgroundService"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await triggerRepository.LoadTriggerToLocationDictionary();
        await cargoRepository.LoadInMemoryCache();
    }
}