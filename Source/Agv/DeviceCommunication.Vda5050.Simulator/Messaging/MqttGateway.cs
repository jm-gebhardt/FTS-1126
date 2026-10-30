using System.Text.Json;
using DeviceCommunication.Vda5050.Simulator.Configuration;
using DeviceCommunication.Vda5050.Simulator.Domain;
using DeviceCommunication.Vda5050.Simulator.Runtime;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using static Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants.Vda5050Specifications;

namespace DeviceCommunication.Vda5050.Simulator.Messaging;

/// <summary>
/// Single MQTT connection. Translates incoming order/instantAction messages into commands for the fleet host
/// and sends the queued outgoing messages.
/// </summary>
public sealed class MqttGateway(IOptions<MqttOptions> options, FleetHost fleet, MqttSendQueue mqttSendQueue, ILogger<MqttGateway> logger) : BackgroundService
{
    private static readonly TimeSpan _reconnectDelay = TimeSpan.FromSeconds(5);
    private readonly MqttOptions _mqtt = options.Value;

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Stop the actors first so that their OFFLINE connection messages are queued before the queue is drained.
        await fleet.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new MqttClientFactory();
        using var client = factory.CreateMqttClient();

        var clientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_mqtt.Host, _mqtt.Port)
            .WithCredentials(_mqtt.Username, _mqtt.Password)
            .WithClientId(_mqtt.ClientId)
            .WithCleanSession()
            .Build();

        client.ApplicationMessageReceivedAsync += e =>
        {
            OnMessageReceived(e.ApplicationMessage.Topic, e.ApplicationMessage.ConvertPayloadToString() ?? string.Empty);
            return Task.CompletedTask;
        };

        var publishLoop = PublishLoopAsync(client, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!client.IsConnected)
                {
                    await client.ConnectAsync(clientOptions, stoppingToken);
                    await SubscribeAsync(factory, client, stoppingToken);
                    logger.LogInformation("Connected to {Host}:{Port}", _mqtt.Host, _mqtt.Port);
                }
                await Task.Delay(_reconnectDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MQTT connection failed, retrying in {Delay}", _reconnectDelay);
                try
                {
                    await Task.Delay(_reconnectDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        await publishLoop;
        await DrainAsync(client);

        if (client.IsConnected)
        {
            await client.DisconnectAsync();
        }
    }

    private async Task PublishLoopAsync(IMqttClient client, CancellationToken stoppingToken)
    {
        var reader = mqttSendQueue.Reader;
        try
        {
            while (await reader.WaitToReadAsync(stoppingToken))
            {
                if (!client.IsConnected)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }
                if (reader.TryRead(out var message))
                {
                    await PublishAsync(client, message, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task DrainAsync(IMqttClient client)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            while (client.IsConnected && mqttSendQueue.Reader.TryRead(out var message))
            {
                await PublishAsync(client, message, timeout.Token);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Timeout while flushing outgoing messages");
        }
    }

    private async Task PublishAsync(IMqttClient client, MqttSendMessage message, CancellationToken cancellationToken)
    {
        var mqttMessage = new MqttApplicationMessageBuilder()
            .WithTopic(message.Topic)
            .WithPayload(message.Payload)
            .WithRetainFlag(message.Retain)
            .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)message.QualityOfService)
            .Build();
        try
        {
            await client.PublishAsync(mqttMessage, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Publishing to {Topic} failed", message.Topic);
        }
    }

    private async Task SubscribeAsync(MqttClientFactory factory, IMqttClient client, CancellationToken cancellationToken)
    {
        var builder = factory.CreateSubscribeOptionsBuilder();
        // Wildcards for manufacturer and serial number, so AGVs that are added at runtime are covered as well.
        builder.WithTopicFilter($"{_mqtt.InterfaceName}/{_mqtt.Version}/+/+/{Subtopics.Order}");
        builder.WithTopicFilter($"{_mqtt.InterfaceName}/{_mqtt.Version}/+/+/{Subtopics.InstantActions}");
        await client.SubscribeAsync(builder.Build(), cancellationToken);
    }

    private void OnMessageReceived(string topic, string payload)
    {
        if (!TopicParser.TryParse(topic, out var id, out string subtopic) || id == null)
        {
            logger.LogWarning("Ignoring message on unexpected topic {Topic}", topic);
            return;
        }

        var command = subtopic switch
        {
            Subtopics.Order => CreateOrderCommand(id, payload),
            Subtopics.InstantActions => CreateInstantActionsCommand(id, payload),
            _ => null
        };

        if (command == null)
        {
            logger.LogWarning("Ignoring message on unsupported subtopic {Topic}", topic);
            return;
        }
        fleet.Dispatch(command);
    }

    private static AgvCommand CreateOrderCommand(AgvIdentity id, string payload)
    {
        MqttOrder? dto;
        try
        {
            dto = JsonSerializer.Deserialize<MqttOrder>(payload, Vda5050Json.Options);
        }
        catch (JsonException ex)
        {
            return new MessageRejected(id,  Subtopics.Order, null, [$"invalid json: {ex.Message}"]);
        }
        if (dto == null)
        {
            return new MessageRejected(id, Subtopics.Order, null, ["empty message"]);
        }
        var result = Vda5050Mapper.MapOrder(dto);
        return result.IsValid
            ? new OrderReceived(id, result.Value!)
            : new MessageRejected(id, Subtopics.Order, dto.OrderId, result.Errors);
    }

    private static AgvCommand CreateInstantActionsCommand(AgvIdentity id, string payload)
    {
        MqttInstantActions? dto;
        try
        {
            dto = JsonSerializer.Deserialize<MqttInstantActions>(payload, Vda5050Json.Options);
        }
        catch (JsonException ex)
        {
            return new MessageRejected(id, Subtopics.InstantActions, null, [$"invalid json: {ex.Message}"]);
        }
        if (dto == null)
        {
            return new MessageRejected(id, Subtopics.InstantActions, null, ["empty message"]);
        }

        var result = Vda5050Mapper.MapInstantActions(dto);
        return result.IsValid
            ? new InstantActionsCommand(id, result.Value!)
            : new MessageRejected(id, Subtopics.InstantActions, null, result.Errors);
    }
}
