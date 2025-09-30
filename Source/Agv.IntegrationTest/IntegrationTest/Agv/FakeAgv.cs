using System.Globalization;
using Gebhardt.Acc.IntegrationTest.Extensions;
using Gebhardt.Acc.IntegrationTest.Mqtt;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Gebhardt.Acc.IntegrationTest.Agv;

/// <summary>
/// Agv, which sends, and responds to, MQTT Messages to simulate a real Agv.
/// </summary>
public class FakeAgv
{
    /// <summary>
    /// Agv Id
    /// </summary>
    public readonly string SerialNumber;

    private readonly MqttClient _mqttClient;
    private readonly ILogger _logger;
    private const int PublishStatusInterval = 1000;
    private JObject? _agvStatus;
    private JObject? _orderEvent;
    private JObject? _agvData;
    private JObject? _graph;
    private JObject? _stationList;
    private JObject? _map;

    /// <summary>
    /// Fake Agv constructor.
    /// </summary>
    /// <param name="serialNumber"></param>
    /// <param name="mqttClient"></param>
    /// <param name="logger"></param>
    public FakeAgv(string serialNumber, MqttClient mqttClient, ILogger logger)
    {
        SerialNumber = serialNumber;
        _mqttClient = mqttClient;
        _logger = logger;
    }

    private void SetCargo(bool hasCargo)
    {
        var loads = _agvStatus!.GetArray("loads");

        JArray? newLoads = null;

        if (hasCargo && (loads == null || !loads.Any()))
        {
            newLoads = [new JObject { ["LoadId"] = Guid.NewGuid() }];
        }
        else if (!hasCargo && loads != null && loads.Any())
        {
            newLoads = [];
        }

        if (newLoads != null)
        {
            _agvStatus!.TrySetArray("loads", newLoads);
            _logger.LogInformation("Setting Cargo to {HasCargo}", hasCargo);
        }
    }

    public async Task StartAsync()
    {
        await _mqttClient.Start();
    }

    /// <summary>
    /// Start sending <see cref="MqttTopics.Status"/> Messages every second.
    /// </summary>
    /// <param name="cancellationToken"></param>
    public void StartSendingStatus(CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                while (cancellationToken is { IsCancellationRequested: false })
                {
                    _logger.LogInformation("Publishing AgvStatus");
                    ChangeTimeStamp(_agvStatus!);
                    await _mqttClient.Publish(MqttTopics.Status, _agvStatus!.ToString(), cancellationToken);
                    await Task.Delay(PublishStatusInterval, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error in sending AgvStatus");
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Subscribe to all topics.
    /// </summary>
    public void SubscribeDataTopics()
    {
        _mqttClient.SubscribeAndRespond(MqttTopics.RequestData, MqttTopics.AgvData, _agvData!, IsCurrentAgv);
        _mqttClient.SubscribeAndRespond(MqttTopics.RequestGraph, MqttTopics.Graph, _graph!, IsCurrentAgv);
        _mqttClient.SubscribeAndRespond(MqttTopics.RequestStationList, MqttTopics.StationList, _stationList!, IsCurrentAgv);
        _mqttClient.SubscribeAndRespond(MqttTopics.RequestGraph, MqttTopics.Map, _map!, IsCurrentAgv);
    }

    /// <summary>
    /// Complete order Successfully.
    /// </summary>
    public void SimulateOrderProcessing()
    {
        _mqttClient.Subscribe(MqttTopics.RequestOrder, async message =>
        {
            try
            {
                var orderId = message["orderId"]?.ToString();
                var nodes = message["route"]?["nodes"];
                _orderEvent!.TryOverwriteValue("OrderId", orderId);

                SetLastNode(nodes?.First()["nodeId"]?.ToString()!);

                _orderEvent!.TryOverwriteValue("OrderEventType", "Started");
                await _mqttClient.Publish(MqttTopics.OrderEvent, _orderEvent!.ToString());
                await Task.Delay(1000);

                SetLastNode(nodes?.Last()["nodeId"]?.ToString()!);
                if (message["orderType"]?.ToString() == "Pick")
                {
                    SetCargo(true);
                }
                else if (message["orderType"]?.ToString() == "Drop")
                {
                    SetCargo(false);
                }

                _orderEvent["OrderEventType"] = "Completed";
                await _mqttClient.Publish(MqttTopics.OrderEvent, _orderEvent.ToString());
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error in sending order event");
            }
        }, IsCurrentAgv);
    }

    private bool IsCurrentAgv(JObject message)
    {
        return message.GetStringValue("serialNumber") == SerialNumber;
    }

    private void ChangeAgvSerialNumber(JObject message)
    {
        message.TryOverwriteValue("serialNumber", SerialNumber);
    }

    private void SetLastNode(string lastNodeName)
    {
        var graphNode = _graph?["nodes"]?.FirstOrDefault(n => n["nodeId"]?.ToString() == lastNodeName);
        ChangeLastNode(_agvStatus!, lastNodeName, (float?) graphNode?["x"], (float?) graphNode?["y"]);
    }

    private void ChangeLastNode(JObject message, string lastNodeName, float? x = null, float? y = null)
    {
        message.TryOverwriteValue("lastNodeId", lastNodeName);
        if (x.HasValue)
        {
            message.TryOverwriteValue("agvPosition.x", x.Value);
        }

        if (y.HasValue)
        {
            message.TryOverwriteValue("agvPosition.y", y.Value);
        }
    }

    private void ChangeTimeStamp(JObject message)
    {
        message.TryOverwriteValue("timeStamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }

    public FakeAgv WithAgvStatusTemplate(JObject agvStatusTemplate)
    {
        _agvStatus = agvStatusTemplate;

        ChangeAgvSerialNumber(_agvStatus);
        SetLastNode("map/NODE-9");
        return this;
    }

    public FakeAgv WithOrderEventTemplate(JObject orderEventTemplate)
    {
        _orderEvent = orderEventTemplate;
        ChangeAgvSerialNumber(_orderEvent);
        return this;
    }

    public FakeAgv WithAgvDataTemplate(JObject agvDataTemplate)
    {
        _agvData = agvDataTemplate;
        ChangeAgvSerialNumber(_agvData);
        return this;
    }

    public FakeAgv WithGraphTemplate(JObject graphTemplate)
    {
        _graph = graphTemplate;
        ChangeAgvSerialNumber(_graph);
        return this;
    }

    public FakeAgv WithStationListTemplate(JObject stationListTemplate)
    {
        _stationList = stationListTemplate;
        ChangeAgvSerialNumber(_stationList);
        return this;
    }

    public FakeAgv WithMapTemplate(JObject mapTemplate)
    {
        _map = mapTemplate;
        ChangeAgvSerialNumber(_map);
        return this;
    }
}