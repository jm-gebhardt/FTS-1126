using Gebhardt.Acc.Common.TelegramCommunication.Communication;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Services.TelegramApi;

/// <summary>
/// Provides a communication manager
/// </summary>
public class ConveyorCommunicationManager(ILogger<ConveyorCommunicationManager> logger, Sender<ConveyorTelegram> sender, Client client) : BackgroundService
{
    private const int ConnectionDelayMs = 10000;

    /// <inheritdoc cref="ConveyorCommunicationManager"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Connect(stoppingToken);
        Start();
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
            logger.LogInformation("Trying to connect to conveyor telegram server");
            initialized =  await client.InitializeAsync();
            await Task.Delay(ConnectionDelayMs, stoppingToken);
        }
       
        sender.RestartRequest += OnRestartRequest!;
    }

    private void Start()
    {
        logger.LogInformation("Starting sender for conveyor communication");
        sender.Start();
    }

    private void Stop()
    {
        logger.LogInformation("Stopping sender for conveyor communication");
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