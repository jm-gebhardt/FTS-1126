using Gebhardt.Acc.IntegrationTest.Extensions;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using Newtonsoft.Json.Linq;

namespace Gebhardt.Acc.IntegrationTest.Mqtt;

/// <summary>
/// The MQTT Client for the integration test.
/// </summary>
public class MqttClient
{
    private const string Host = "localhost";
    private readonly IManagedMqttClient _managedMqttClient;
    private readonly IMqttClient _mqttClient;

    private readonly string _user;
    private readonly string _password;
    private readonly ILogger _logger;


    /// <summary>
    /// Constructor
    /// </summary>
    public MqttClient(string user, string password, ILogger logger)
    {
        _user = user;
        _password = password;
        _logger = logger;
        var factory = new MqttFactory();
        _managedMqttClient = factory.CreateManagedMqttClient();
        _mqttClient = factory.CreateMqttClient();
    }

    /// <summary>
    /// Start Mqtt client.
    /// </summary>
    public async Task Start()
    {
        if (!_managedMqttClient.IsStarted)
        {
            await _managedMqttClient.StartAsync(ManagedClientOptions());
        }

        if (!_mqttClient.IsConnected)
        {
            await _mqttClient.ConnectAsync(ClientOptions());
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
        _managedMqttClient.ApplicationMessageReceivedAsync += async msg =>
        {
            var messageContent = msg.ApplicationMessage.ConvertPayloadToString();
            var jObject = JObject.Parse(messageContent);
            if (!filter(jObject) || msg.ApplicationMessage.Topic != topic)
            {
                return;
            }

            _logger.LogInformation("Received message on topic: {Topic}, Message: {Message}", msg.ApplicationMessage.Topic,
                messageContent);
            await callback.Invoke(jObject);
        };
        _managedMqttClient.SubscribeAsync(topic);
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

    private ManagedMqttClientOptions ManagedClientOptions()
    {
        return new ManagedMqttClientOptionsBuilder()
            .WithClientOptions(ClientOptions())
            .Build();
    }
}