using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Services;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.KaeserSidecar.Domain.Configuration;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gebhardt.Acc.KaeserSidecar.Services.BackgroundServices;

/// <summary>
/// Provides initialization for the service.
/// </summary>
[UsedImplicitly]
public class InitializationProvider(
    [FromKeyedServices(ServiceKeyProvider.AplWeStation)]
    ObservableConcurrentQueue<ConveyorTelegram> agvWeStationQueue,
    [FromKeyedServices(ServiceKeyProvider.AgvCargoState)]
    TelegramQueuePersistenceService agvCargoStatePersistenceService,
    [FromKeyedServices(ServiceKeyProvider.AgvCargoState)]
    ObservableConcurrentQueue<ConveyorTelegram> agvCargoStateQueue,
    [FromKeyedServices(ServiceKeyProvider.AplWeStation)]
    TelegramQueuePersistenceService agvWeStationPersistenceService) : BackgroundService
{
    /// <inheritdoc cref="BackgroundService"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        agvCargoStateQueue.ItemDequeued += agvCargoStatePersistenceService.OnItemDequeued;
        agvCargoStateQueue.ItemEnqueued += agvCargoStatePersistenceService.OnItemEnqueued;
        agvWeStationQueue.ItemDequeued += agvWeStationPersistenceService.OnItemDequeued;
        agvWeStationQueue.ItemEnqueued += agvWeStationPersistenceService.OnItemEnqueued;
        await Task.WhenAll(agvCargoStatePersistenceService.ReloadQueue(), agvWeStationPersistenceService.ReloadQueue());
    }
}