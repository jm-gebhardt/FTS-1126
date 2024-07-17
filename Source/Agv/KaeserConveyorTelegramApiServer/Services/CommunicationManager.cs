using Gebhardt.Acc.Common.TelegramCommunication.Communication;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserConveyorTelegramApiServer.Services;

/// <summary>
/// Provides a communication manager
/// </summary>
public class CommunicationManager(ILogger<CommunicationManager> logger, Receiver<ConveyorTelegram> receiver, Server server) : BackgroundService
{
    /// <inheritdoc cref="CommunicationManager"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
       Connect();
       Start();
       while (!stoppingToken.IsCancellationRequested)
       {
           await Task.Delay(1000, stoppingToken);
       }

       Stop();  
    }

    private void Connect()
    {
        server.Initialize();
        receiver.RestartRequest += OnRestartRequest!;
    }

    private void Start()
    {
        logger.LogInformation("Starting sender for conveyor communication");
        receiver.Start();
    }

    private void Stop()
    {
        logger.LogInformation("Stopping sender for conveyor communication");
        receiver.BeginStop();
        receiver.FinishStop();
        server.Disconnect();
    }

    /// <summary>
    /// Handle restart request
    /// </summary>
    /// <param name="sender">The sender of the event</param>
    /// <param name="eventArgs">The arguments of the event</param>
    private void OnRestartRequest(object sender, EventArgs eventArgs)
    {
        if (sender is not SocketCommunicator<ConveyorTelegram> communicator)
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