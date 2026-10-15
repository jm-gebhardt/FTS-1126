using System.Text.Json;
using Gebhardt.Acc.Vda5050Simulator.Configuration;
using Gebhardt.Acc.Vda5050Simulator.Domain;
using Gebhardt.Acc.Vda5050Simulator.Messaging.Dtos;
using Gebhardt.Acc.Vda5050Simulator.Runtime;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;

namespace Gebhardt.Acc.Vda5050Simulator.Messaging;

/// <summary>
/// Single MQTT connection. Translates incoming order/instantAction messages into commands for the fleet host
/// and sends the queued outgoing messages.
/// </summary>
public sealed class MqttGateway(
    IOptions<MqttOptions> options,
    FleetHost fleet,
    OutboundQueue outbound,
    ILogger<MqttGateway> logger) : BackgroundService
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
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
            OnMessage(e.ApplicationMessage.Topic, e.ApplicationMessage.ConvertPayloadToString() ?? string.Empty);
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
                await Task.Delay(ReconnectDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MQTT connection failed, retrying in {Delay}", ReconnectDelay);
                try
                {
                    await Task.Delay(ReconnectDelay, stoppingToken);
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
        var reader = outbound.Reader;
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
            while (client.IsConnected && outbound.Reader.TryRead(out var message))
            {
                await PublishAsync(client, message, timeout.Token);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Timeout while flushing outgoing messages");
        }
    }

    private async Task PublishAsync(IMqttClient client, OutboundMessage message, CancellationToken cancellationToken)
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
        foreach (var id in fleet.AgvIds)
        {
            builder.WithTopicFilter(TopicParser.BuildTopic(_mqtt.InterfaceName, _mqtt.Version, id, TopicParser.Order));
            builder.WithTopicFilter(TopicParser.BuildTopic(_mqtt.InterfaceName, _mqtt.Version, id, TopicParser.InstantAction));
        }
        await client.SubscribeAsync(builder.Build(), cancellationToken);
    }

    private void OnMessage(string topic, string payload)
    {
        if (!TopicParser.TryParse(topic, out var id, out var subtopic))
        {
            logger.LogWarning("Ignoring message on unexpected topic {Topic}", topic);
            return;
        }

        var command = subtopic switch
        {
            TopicParser.Order => CreateOrderCommand(id, payload),
            TopicParser.InstantAction => CreateInstantActionsCommand(id, payload),
            _ => null
        };

        if (command == null)
        {
            logger.LogWarning("Ignoring message on unsupported subtopic {Topic}", topic);
            return;
        }
        fleet.Dispatch(command);
    }

    private static AgvCommand CreateOrderCommand(AgvId id, string payload)
    {
        OrderDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<OrderDto>(payload, JsonOptions);
        }
        catch (JsonException ex)
        {
            return new MessageRejected(id, "order", null, [$"invalid json: {ex.Message}"]);
        }
        if (dto == null)
        {
            return new MessageRejected(id, "order", null, ["empty message"]);
        }
        var result = Vda5050Mapper.MapOrder(dto);
        return result.IsValid
            ? new OrderReceived(id, result.Value!)
            : new MessageRejected(id, "order", dto.OrderId, result.Errors);
    }

    private static AgvCommand CreateInstantActionsCommand(AgvId id, string payload)
    {
        InstantActionsDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<InstantActionsDto>(payload, JsonOptions);
        }
        catch (JsonException ex)
        {
            return new MessageRejected(id, "instantActions", null, [$"invalid json: {ex.Message}"]);
        }
        if (dto == null)
        {
            return new MessageRejected(id, "instantActions", null, ["empty message"]);
        }
        var result = Vda5050Mapper.MapInstantActions(dto);
        return result.IsValid
            ? new InstantActionsReceived(id, result.Value!)
            : new MessageRejected(id, "instantActions", null, result.Errors);
    }
}
