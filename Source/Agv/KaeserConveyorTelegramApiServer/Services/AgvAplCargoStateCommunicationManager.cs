using Gebhardt.Acc.Common.TelegramCommunication.Communication;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Telegrams;
using Gebhardt.Acc.KaeserSidecar.Domain.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserConveyorTelegramApiServer.Services;

/// <summary>
/// Provides a communication manager
/// </summary>
public class AgvAplCargoStateCommunicationManager(ILogger<AgvAplCargoStateCommunicationManager> logger, [FromKeyedServices(ServiceKeyProvider.AgvCargoState)] Receiver<ConveyorTelegram> receiver, [FromKeyedServices(ServiceKeyProvider.AgvCargoState)] Server server) : BackgroundService
{
    /// <inheritdoc cref="AgvAplCargoStateCommunicationManager"/>>
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
        logger.LogInformation("Starting sender for conveyor communication with {Server} server", ServiceKeyProvider.AgvCargoState);
        receiver.Start();
    }

    private void Stop()
    {
        logger.LogInformation("Stopping sender for conveyor communication with {Server} server", ServiceKeyProvider.AgvCargoState);
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
        _ = HandleRestartRequestAsync(sender, eventArgs);
    }

    private async Task HandleRestartRequestAsync(object sender, EventArgs _)
    {
        if (sender is not SocketCommunicator<ConveyorTelegram> communicator)
        {
            return;
        }

        logger.LogInformation("Restart requested from {Name}", communicator.Name);

        if (communicator.RestartMe)
        {
            logger.LogInformation("Restarting {Name}", communicator.Name);
            await communicator.RestartAsync(); 
        }
    }
}