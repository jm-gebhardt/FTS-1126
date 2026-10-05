using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.Common.TelegramCommunication.Communication;
using Gebhardt.Acc.Common.TelegramCommunication.Configuration;
using Gebhardt.Acc.Common.TelegramCommunication.Telegram;
using Gebhardt.Acc.StationCommunication.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.StationCommunication.Services.TelegramApi;

/// <summary>
/// Base class for PLC communication managers that tracks connection state and publishes station online/offline events.
/// </summary>
/// <typeparam name="TCommunicator">The communicator type (<see cref="Sender{TTelegramType}"/> or <see cref="Receiver{TTelegramType}"/>).</typeparam>
/// <typeparam name="TTelegramType">The telegram type handled by the communicator.</typeparam>
public abstract class PlcCommunicationManagerBase<TCommunicator, TTelegramType>(
    string channelKey,
    TCommunicator communicator,
    SocketBase client,
    TelegramCommunicationConfiguration configuration,
    IPlcConnectionRepository connectionRepository,
    IBus bus,
    ILogger logger) : BackgroundService
    where TCommunicator : SocketCommunicator<TTelegramType>
    where TTelegramType : TelegramBase
{
    private const int ConnectionDelayMs = 10000;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.UseConnection)
        {
            logger.LogInformation("Channel {ChannelKey} ({IpAddress}:{Port}) not used, skipping", channelKey, configuration.IpAddress, configuration.Port);
            return;
        }

        communicator.ConnectionStateChanged = OnConnectionStateChanged;

        logger.LogInformation("Waiting for PLC connection repository initialization before connecting to channel {ChannelKey} ({IpAddress}:{Port})", channelKey, configuration.IpAddress, configuration.Port);
        await connectionRepository.WaitForInitializationAsync(stoppingToken);
        logger.LogInformation("PLC connection repository initialized, connecting to channel {ChannelKey} ({IpAddress}:{Port})", channelKey, configuration.IpAddress, configuration.Port);

        await ConnectAsync(stoppingToken);
        logger.LogInformation("Starting communicator for channel {ChannelKey} ({IpAddress}:{Port})", channelKey, configuration.IpAddress, configuration.Port);
        communicator.Start();

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }

        logger.LogInformation("Stopping communicator for channel {ChannelKey} ({IpAddress}:{Port})", channelKey, configuration.IpAddress, configuration.Port);
        await communicator.StopAsync();
        client.Disconnect();
    }

    private async Task ConnectAsync(CancellationToken stoppingToken)
    {
        bool initialized = await TryConnectAsync();

        if (!initialized)
        {
            await PublishInitialOfflineAsync();
        }

        while (!initialized && !stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(ConnectionDelayMs, stoppingToken);
            initialized = await TryConnectAsync();
        }
    }

    private async Task<bool> TryConnectAsync()
    {
        logger.LogInformation("Trying to connect to {ChannelKey} ({IpAddress}:{Port})", channelKey, configuration.IpAddress, configuration.Port);
        return await client.TryInitializeAsync();
    }

    private async Task PublishInitialOfflineAsync()
    {
        foreach ((var stationId, bool isOnline) in connectionRepository.UpdateChannelState(channelKey, false, includeUntracked: true))
        {
            await bus.Publish(new StationIsOnlineChanged(stationId, isOnline));
            logger.LogWarning("Station {StationId} went offline. Channel {ChannelKey} ({IpAddress}:{Port}) is unavailable at startup",
                stationId, channelKey, configuration.IpAddress, configuration.Port);
        }
    }

    private void OnConnectionStateChanged(bool isConnected)
    {
        _ = HandleConnectionStateChangedAsync(isConnected);
    }

    private async Task HandleConnectionStateChangedAsync(bool isConnected)
    {
        try
        {
            var changed = connectionRepository.UpdateChannelState(channelKey, isConnected);

            foreach ((var stationId, bool isOnline) in changed)
            {
                await bus.Publish(new StationIsOnlineChanged(stationId, isOnline));

                if (isOnline)
                {
                    logger.LogInformation("Station {StationId} came online. Channel {ChannelKey} ({IpAddress}:{Port}) connected",
                        stationId, channelKey, configuration.IpAddress, configuration.Port);
                }
                else
                {
                    logger.LogWarning("Station {StationId} went offline. Channel {ChannelKey} ({IpAddress}:{Port}) is lost",
                        stationId, channelKey, configuration.IpAddress, configuration.Port);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle connection state change for channel {ChannelKey}", channelKey);
        }
    }
}