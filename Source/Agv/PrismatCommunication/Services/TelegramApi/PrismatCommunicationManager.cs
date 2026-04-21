using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.TelegramCommunication.Communication;
using Gebhardt.Acc.PrismatCommunication.TelegramApi;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Processors;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Services;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Services.TelegramApi;

/// <summary>
/// Provides a communication manager
/// </summary>
public class PrismatCommunicationManager : BackgroundService
{
    private readonly ILogger<PrismatCommunicationManager> _logger;
    private readonly Receiver<PrismatTelegram> _receiver;
    private readonly Sender<PrismatTelegram> _sender;
    private readonly Server _fromPrismatServer;
    private readonly Server _toPrismatServer;
    private readonly TelegramQueuePersistenceService _queuePersistenceService;
    private readonly ObservableConcurrentQueue<PrismatTelegram> _prismatQueue;
    private readonly ToPrismatTelegramProcessor _toPrismatProcessor;

    /// <inheritdoc cref="PrismatCommunicationManager"/>>
    public PrismatCommunicationManager(ILogger<PrismatCommunicationManager> logger,
        Receiver<PrismatTelegram> receiver,
        Sender<PrismatTelegram> sender,
        [FromKeyedServices(ServiceKeyProvider.FromPrismat)]
        Server fromPrismatServer,
        [FromKeyedServices(ServiceKeyProvider.ToPrismat)]
        Server toPrismatServer,
        TelegramQueuePersistenceService queuePersistenceService,
        ObservableConcurrentQueue<PrismatTelegram> prismatQueue,
        ToPrismatTelegramProcessor toPrismatProcessor)
    {
        _logger = logger;
        _receiver = receiver;
        _sender = sender;
        _fromPrismatServer = fromPrismatServer;
        _toPrismatServer = toPrismatServer;
        _queuePersistenceService = queuePersistenceService;
        _prismatQueue = prismatQueue;
        _toPrismatProcessor = toPrismatProcessor;
    }

    /// <inheritdoc cref="PrismatCommunicationManager"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.WhenAll(StartAndConnectReceivingChannel(), StartAndConnectSendingChannel());
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        await StopAsync();
    }

    private async Task StartAndConnectReceivingChannel()
    {
        await _fromPrismatServer.TryInitializeAsync();
        _logger.LogInformation("Starting receiver for prismat communication");
        _receiver.Start();
    }

    private async Task StartAndConnectSendingChannel()
    {
        await _toPrismatServer.TryInitializeAsync();
        _toPrismatProcessor.TelegramAcknowledged += async (_, item) =>
        {
            try
            {
                await _queuePersistenceService.OnTelegramAcknowledged(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete telegram from persistence");
            }
        };
        _prismatQueue.ItemEnqueued += async (_, item) =>
        {
            try
            {
                await _queuePersistenceService.OnTelegramEnqueued(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist telegram");
            }
        };
        await _queuePersistenceService.ReloadQueue();
        _logger.LogInformation("Starting sender for prismat communication");
        _sender.Start();
    }

    private async Task StopAsync()
    {
        _logger.LogInformation("Stopping sender and receiver for prismat communication");
        await _receiver.StopAsync();
        await _sender.StopAsync();
    }
}