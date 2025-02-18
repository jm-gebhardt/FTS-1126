using Gebhardt.Acc.Common.TelegramCommunication.Communication;
using Gebhardt.Acc.PrismatCommunication.TelegramApi;
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

    /// <inheritdoc cref="PrismatCommunicationManager"/>>
    public PrismatCommunicationManager(ILogger<PrismatCommunicationManager> logger, Receiver<PrismatTelegram> receiver, Sender<PrismatTelegram> sender, [FromKeyedServices(ServiceKeyProvider.FromPrismat)] Server fromPrismatServer, [FromKeyedServices(ServiceKeyProvider.ToPrismat)] Server toPrismatServer)
    {
        _logger = logger;
        _receiver = receiver;
        _sender = sender;
        _fromPrismatServer = fromPrismatServer;
        _toPrismatServer = toPrismatServer;
    }

    /// <inheritdoc cref="PrismatCommunicationManager"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Connect();
        Start();
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        Stop();
    }

    private async Task Connect()
    {
        await Task.WhenAll(_fromPrismatServer.InitializeAsync(), _toPrismatServer.InitializeAsync());
        _receiver.RestartRequest += OnRestartRequest!;
        _sender.RestartRequest += OnRestartRequest!;
    }

    private void Start()
    {
        _logger.LogInformation("Starting sender and receiver for prismat communication");
        _receiver.Start();
        _sender.Start();
    }

    private void Stop()
    {
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