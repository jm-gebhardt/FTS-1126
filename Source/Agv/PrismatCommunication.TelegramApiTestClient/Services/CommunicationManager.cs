using Gebhardt.Acc.Common.TelegramCommunication.Communication;
using Gebhardt.Acc.PrismatCommunication.TelegramApi.Telegrams;
using Gebhardt.Acc.PrismatCommunication.TelegramApiClient.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.TelegramApiClient.Services;

/// <summary>
/// Provides a communication manager
/// </summary>
public class CommunicationManager : BackgroundService
{
    private readonly ILogger<CommunicationManager> _logger;
    private readonly Receiver<PrismatTelegram> _receiver;
    private readonly Sender<PrismatTelegram> _sender;
    private readonly Client _toFtsClient;
    private readonly Client _fromFtsClient;

    /// <inheritdoc cref="CommunicationManager"/>>
    public CommunicationManager(ILogger<CommunicationManager> logger, Receiver<PrismatTelegram> receiver, Sender<PrismatTelegram> sender, [FromKeyedServices(ServiceKeyProvider.ToFts)] Client toFtsClient, [FromKeyedServices(ServiceKeyProvider.FromFts)] Client fromFtsClient)
    {
        _logger = logger;
        _receiver = receiver;
        _sender = sender;
        _toFtsClient = toFtsClient;
        _fromFtsClient = fromFtsClient;
    }

    /// <inheritdoc cref="CommunicationManager"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting sender and receiver for prismat communication");
        bool connected = false;
        while (!connected)
        {
            connected = await _toFtsClient.TryInitializeAsync();
            await Task.Delay(1000, stoppingToken);
        }
        connected = false;
        while (!connected)
        {
            connected = await _fromFtsClient.TryInitializeAsync();
            await Task.Delay(1000, stoppingToken);
        }
        _receiver.Start();
        _sender.Start();

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
        _logger.LogInformation("Stopping sender and receiver for prismat communication");
        await _receiver.StopAsync();
        await _sender.StopAsync();
    }
}