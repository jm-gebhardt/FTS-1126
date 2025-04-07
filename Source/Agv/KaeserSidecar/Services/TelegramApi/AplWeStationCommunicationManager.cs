using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.TelegramCommunication.Communication;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Services;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.KaeserSidecar.Domain.Configuration;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Services.TelegramApi;

/// <summary>
/// Provides a communication manager
/// </summary>
[UsedImplicitly]
public class AplWeStationCommunicationManager(
    ILogger<AplWeStationCommunicationManager> logger,
    [FromKeyedServices(ServiceKeyProvider.AplWeStation)]
    Sender<ConveyorTelegram> sender,
    [FromKeyedServices(ServiceKeyProvider.AplWeStation)]
    ObservableConcurrentQueue<ConveyorTelegram> agvWeStationQueue,
    [FromKeyedServices(ServiceKeyProvider.AplWeStation)]
    TelegramQueuePersistenceService agvWeStationPersistenceService,
    [FromKeyedServices(ServiceKeyProvider.AplWeStation)]
    Client client) : BackgroundService
{
    private const int ConnectionDelayMs = 10000;

    /// <inheritdoc cref="AplWeStationCommunicationManager"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Connect(stoppingToken);
        Start();
        await agvWeStationPersistenceService.ReloadQueue();
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        Stop();
    }

    private async Task Connect(CancellationToken stoppingToken)
    {
        bool initialized = false;
        while (!initialized)
        {
            logger.LogInformation("Trying to connect to {Server} telegram server", ServiceKeyProvider.AplWeStation);
            initialized =  await client.InitializeAsync();
            await Task.Delay(ConnectionDelayMs, stoppingToken);
        }
       
        sender.RestartRequest += OnRestartRequest!;
        agvWeStationQueue.ItemDequeued += agvWeStationPersistenceService.OnItemDequeued;
        agvWeStationQueue.ItemEnqueued += agvWeStationPersistenceService.OnItemEnqueued;
    }

    private void Start()
    {
        logger.LogInformation("Starting sender for conveyor communication for {Server}", ServiceKeyProvider.AplWeStation);
        sender.Start();
    }

    private void Stop()
    {
        logger.LogInformation("Stopping sender for conveyor communication for {Server}", ServiceKeyProvider.AplWeStation);
        sender.BeginStop();
        sender.FinishStop();
        client.Disconnect();
    }

    /// <summary>
    /// Handle restart request
    /// </summary>
    /// <param name="issuer">The issuer of the event</param>
    /// <param name="eventArgs">The arguments of the event</param>
    private void OnRestartRequest(object issuer, EventArgs eventArgs)
    {
        if (issuer is not SocketCommunicator<ConveyorTelegram> communicator)
        {
            return;
        }
        logger.LogInformation("Restart requested from {Name}", communicator.Name);
        if (communicator.RestartMe)
        {
            communicator.Restart();
        }
    }
}