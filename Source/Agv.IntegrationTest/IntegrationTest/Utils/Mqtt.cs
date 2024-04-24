using Microsoft.IdentityModel.Tokens;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using Newtonsoft.Json.Linq;

namespace Gebhardt.Acc.IntegrationTest.Utils;

/// <summary>
/// The MQTT Client for the integration test.
/// </summary>
public class MqttClient
{
    private readonly MqttFactory _factory;
    private readonly IManagedMqttClient _managedMqttClient;
    private const string Host = "localhost";

    private readonly string? _mqttSamplePath =
        Environment.GetEnvironmentVariable(EnvVars.MqttSamplePath) 
        ?? throw new InvalidOperationException($"Environment variable {EnvVars.MqttSamplePath} must be set.");

    /// <summary>
    /// Mqtt Topics
    /// </summary>
    public static class Topic
    {
        /// <summary>
        /// Agv Status
        /// </summary>
        public const string Status = "agv/status";

        /// <summary>
        /// Agv Data
        /// </summary>
        public const string AgvData = "agv/agvData";

        /// <summary>
        /// Agv Data
        /// </summary>
        public const string Graph = "agv/graph";

        /// <summary>
        /// Agv Data
        /// </summary>
        public const string StationList = "agv/stationList";

        /// <summary>
        /// Agv Data
        /// </summary>
        public const string Map = "agv/map";

        /// <summary>
        /// Order event
        /// </summary>
        public const string OrderEvent = "agv/orderEvent";

        /// <summary>
        /// Data requested
        /// </summary>
        public const string RequestData = "agv/requestAgvData";

        /// <summary>
        /// Graph requested
        /// </summary>
        public const string RequestGraph = "agv/requestGraph";

        /// <summary>
        /// StationList requested
        /// </summary>
        public const string RequestStationList = "agv/requestStationList";

        /// <summary>
        /// Map requested
        /// </summary>
        public const string RequestMap = "agv/requestMap";

        /// <summary>
        /// Order requested
        /// </summary>
        public const string RequestOrder = "agv/order";
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public MqttClient()
    {
        _factory = new MqttFactory();
        _managedMqttClient = _factory.CreateManagedMqttClient();
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
    }


    /// <summary>
    /// Subscribe to given topic.
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="callback"></param>
    public void Subscribe(string topic, Action<JToken>? callback = null)
    {
        _managedMqttClient.ApplicationMessageReceivedAsync += msg =>
        {
            if (msg.ApplicationMessage.Topic == topic)
            {
                Console.WriteLine($"Received: {msg.ApplicationMessage.Topic}");
                Console.WriteLine(msg.ApplicationMessage.ConvertPayloadToString());
                var response = JToken.Parse(msg.ApplicationMessage.ConvertPayloadToString());
                if (!response.IsNullOrEmpty())
                {
                    callback?.Invoke(response);
                }
            }

            return Task.CompletedTask;
        };
        _managedMqttClient.SubscribeAsync(topic);
    }

    /// <summary>
    /// Subscribe to topic and respond with json provided in fileName.
    /// Properties in request are camelCase, properties in response are PascalCase.
    /// </summary>
    /// <param name="receiveTopic"></param>
    /// <param name="respondTopic"></param>
    /// <param name="fileName">Response Json File</param>
    /// <param name="requestFilter">Message requestFilter</param>
    /// <param name="callback">callback to allow manipulation of response json</param>
    public void SubscribeAndRespond(string receiveTopic, string respondTopic, string fileName,
        Func<JToken, bool> requestFilter, Action<JToken>? callback = null)
    {
        async void Callback(JToken request)
        {
            if (requestFilter(request))
            {
                var response = ReadSample(fileName);
                response["requestId"] = request["requestId"]?.ToString();
                callback?.Invoke(response);
                await Publish(respondTopic, response.ToString());
            }
        }

        Subscribe(receiveTopic, Callback);
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
        using var mqttClient = _factory.CreateMqttClient();
        await mqttClient.ConnectAsync(ClientOptions(), cancellationToken);

        var applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(message)
            .Build();

        await mqttClient.PublishAsync(applicationMessage, cancellationToken);
        await mqttClient.DisconnectAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Read example file.
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns>parsed json object</returns>
    public JObject ReadSample(string fileName)
    {
        var filePath = $"{_mqttSamplePath}/{fileName}";
        if(!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Could not find MQTT sample file: '{fileName}'");
        }
            
        return JObject.Parse(File.ReadAllText(filePath));
    }

    private MqttClientOptions ClientOptions()
    {
        return new MqttClientOptionsBuilder()
            .WithTcpServer(Host)
            .WithCredentials(EnvFile.DefaultUser, EnvFile.DefaultPassword)
            .Build();
    }

    private ManagedMqttClientOptions ManagedClientOptions()
    {
        return new ManagedMqttClientOptionsBuilder()
            .WithClientOptions(ClientOptions())
            .Build();
    }
}