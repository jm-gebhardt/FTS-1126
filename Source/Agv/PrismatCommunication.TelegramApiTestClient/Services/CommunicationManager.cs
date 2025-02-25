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
            connected = await _toFtsClient.InitializeAsync();
            await Task.Delay(1000, stoppingToken);
        }
        connected = false;
        while (!connected)
        {
            connected = await _fromFtsClient.InitializeAsync();
            await Task.Delay(1000, stoppingToken);
        }
        _receiver.RestartRequest += OnRestartRequest!;
        _receiver.Start();
        _sender.Start();
        _sender.RestartRequest += OnRestartRequest!;

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
        _logger.LogInformation("Stopping sender and receiver for prismat communication");
        _receiver.BeginStop();
        _sender.BeginStop();
        _receiver.FinishStop();
        _sender.FinishStop();
    }

    /// <summary>
    /// Handle restart request
    /// </summary>
    /// <param name="sender">The sender of the event</param>
    /// <param name="eventArgs">The arguments of the event</param>
    private void OnRestartRequest(object sender, EventArgs eventArgs)
    {
        if (sender is not SocketCommunicator<PrismatTelegram> communicator)
        {
            return;
        }
        _logger.LogInformation("Restart requested from {Name}", communicator.Name);
        if (communicator.RestartMe)
        {
            communicator.Restart();
        }
    }
}