using Gebhardt.Acc.IntegrationTest.Extensions;
using Microsoft.Extensions.Logging;
using MQTTnet;
using Newtonsoft.Json.Linq;

namespace Gebhardt.Acc.IntegrationTest.Mqtt;

/// <summary>
/// The MQTT Client for the integration test.
/// </summary>
public class MqttClient : IAsyncDisposable
{
    private const string Host = "localhost";
    private readonly IMqttClient _mqttClient;
    private readonly string _user;
    private readonly string _password;
    private readonly ILogger _logger;

    public MqttClient(string user, string password, ILogger logger)
    {
        _user = user;
        _password = password;
        _logger = logger;
        var factory = new MqttClientFactory();
        _mqttClient = factory.CreateMqttClient();
    }

    /// <summary>
    /// Start Mqtt client.
    /// </summary>
    public async Task Start()
    {
        if (!_mqttClient.IsConnected)
        {
            var result = await _mqttClient.ConnectAsync(ClientOptions());

            if (result.ResultCode != MqttClientConnectResultCode.Success)
            {
                _logger.LogError("Failed to connect: {ResultCode}, Reason: {ReasonString}",
                    result.ResultCode, result.ReasonString);
                throw new InvalidOperationException($"MQTT connection failed: {result.ResultCode}");
            }

            _logger.LogInformation("Successfully connected to MQTT broker");
        }
    }

    /// <summary>
    /// Subscribe to given topic.
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="callback"></param>
    /// <param name="filter"></param>
    public void Subscribe(string topic, Func<JObject, Task> callback, Func<JObject, bool> filter)
    {
        _mqttClient.ApplicationMessageReceivedAsync += async msg =>
        {
            var messageContent = msg.ApplicationMessage.ConvertPayloadToString();
            var jObject = JObject.Parse(messageContent);
            if (!filter(jObject) || msg.ApplicationMessage.Topic != topic)
            {
                return;
            }

            _logger.LogInformation("Received message on topic: {Topic}, Message: {Message}", msg.ApplicationMessage.Topic, messageContent);
            await callback.Invoke(jObject);
        };

        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(topic)
            .Build();

        _mqttClient.SubscribeAsync(subscribeOptions);
    }

    /// <summary>
    /// Subscribe to topic and respond with json provided in fileName.
    /// Properties in request are camelCase, properties in response are PascalCase.
    /// </summary>
    /// <param name="receiveTopic"></param>
    /// <param name="respondTopic"></param>
    /// <param name="message">Response Json File</param>
    /// <param name="requestFilter">Message requestFilter</param>
    public void SubscribeAndRespond(string receiveTopic, string respondTopic, JObject message, Func<JObject, bool> requestFilter)
    {
        Subscribe(receiveTopic, async request =>
        {
            message.TryOverwriteValue("requestId", request["requestId"]?.ToString());
            await Publish(respondTopic, message.ToString());
        }, requestFilter);
    }

    /// <summary>
    /// Publish Mqtt message.
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task Publish(string topic, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .Build();

            await _mqttClient.PublishAsync(applicationMessage, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private MqttClientOptions ClientOptions()
    {
        return new MqttClientOptionsBuilder()
            .WithTcpServer(Host)
            .WithCredentials(_user, _password)
            .Build();
    }

    public async ValueTask DisposeAsync()
    {
        if (_mqttClient.IsConnected)
        {
            await _mqttClient.DisconnectAsync(new MqttClientDisconnectOptions());
            _logger.LogInformation("Disconnected from MQTT broker");
        }

        _mqttClient.Dispose();
        GC.SuppressFinalize(this);
    }
}