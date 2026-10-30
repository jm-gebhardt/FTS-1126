using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using Gebhardt.Acc.Vda5050Simulator.Configuration;
using Gebhardt.Acc.Vda5050Simulator.Domain;
using Gebhardt.Acc.Vda5050Simulator.Runtime;
using MassTransit.Serialization;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Threading.Channels;

namespace Gebhardt.Acc.Vda5050Simulator.Messaging;

public sealed record MqttSendMessage(string Topic, string Payload, bool Retain, int QualityOfService);

/// <summary>
/// Maps domain snapshots to VDA5050 messages and queues them for the single publisher task of the gateway.
/// When the queue is full (e.g. broker unreachable), the oldest message is dropped.
/// </summary>
public sealed class MqttSendQueue(IOptions<MqttOptions> options) : IAgvPublisher
{
    private const int Capacity = 10_000;

    private readonly MqttOptions _mqtt = options.Value;

    private readonly Channel<MqttSendMessage> _channel = Channel.CreateBounded<MqttSendMessage>(
        new BoundedChannelOptions(Capacity) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public ChannelReader<MqttSendMessage> Reader => _channel.Reader;

    public void PublishConnection(AgvIdentity id, ConnectionState state, uint headerId, DateTimeOffset timestamp)
    {
        var message = new MqttConnection
        {
            HeaderId = headerId,
            Timestamp = timestamp,
            Version = _mqtt.ProtocolVersion,
            Manufacturer = id.Manufacturer,
            SerialNumber = id.SerialNumber,
            ConnectionState = Enum.Parse<MqttConnectionState>(state.ToString())
        };
        Enqueue(id, Vda5050Specifications.Subtopics.Connection , message, retain: true, qualityOfService: 1);
    }

    public void PublishState(AgvIdentity id, AgvStateSnapshot snapshot)
    {
        var message = new MqttState
        {
            HeaderId = (uint)snapshot.HeaderId,
            Timestamp = snapshot.Timestamp,
            Version = _mqtt.ProtocolVersion,
            Manufacturer = id.Manufacturer,
            SerialNumber = id.SerialNumber,
            OrderId = snapshot.OrderId,
            OrderUpdateId = snapshot.OrderUpdateId,
            LastNodeId = snapshot.LastNodeId,
            LastNodeSequenceId = snapshot.LastNodeSequenceId,
            Driving = snapshot.Driving,
            OperatingMode = Enum.Parse<MqttOperatingMode>(snapshot.OperatingMode.ToString()),

            AgvPosition = snapshot.Position is { } p ? new MqttAgvPosition { X = p.X, Y = p.Y, Theta = p.Theta, MapId = p.MapId, PositionInitialized = p.PositionInitialized } : null,

            Velocity = new MqttVelocity { Vx = snapshot.Velocity.Vx, Vy = snapshot.Velocity.Vy, Omega = snapshot.Velocity.Omega },

            Loads = snapshot.Loads
                .Select(l => new MqttLoad { LoadId = l.LoadId, LoadType = l.LoadType, LoadPosition = l.LoadPosition })
                .ToList(),

            ActionStates = snapshot.ActionStates
                .Select(a => new MqttActionState
                {
                    ActionId = a.ActionId,
                    ActionType = a.ActionType,
                    ActionStatus = Enum.Parse<MqttActionStatus>(a.Status.ToString())
                })
                .ToList(),

            BatteryState = new MqttBatteryState { BatteryCharge = snapshot.BatteryCharge, Charging = snapshot.Charging },

            Errors = snapshot.Errors
                .Select(e => new MqttError
                {
                    ErrorType = e.ErrorType,
                    ErrorLevel = Enum.Parse<MqttErrorLevel>(e.Level.ToString()),
                    ErrorDescription = e.Description,
                    ErrorReferences = e.ErrorReferences?
                        .Select(r => new MqttErrorReference { ReferenceKey = r.Key, ReferenceValue = r.Value })
                        .ToList()
                })
                .ToList(),

            NodeStates = snapshot.NodeStates
                .Select(n => new MqttNodeState { NodeId = n.NodeId, SequenceId = n.SequenceId, Released = n.Released })
                .ToList(),

            EdgeStates = snapshot.EdgeStates
                .Select(e => new MqttEdgeState { EdgeId = e.EdgeId, SequenceId = e.SequenceId, Released = e.Released })
                .ToList(),

            SafetyState = new MqttSafetyState { EStop = MqttEStop.None, FieldViolation = false }
        };

        Enqueue(id, Vda5050Specifications.Subtopics.State, message, retain: false, qualityOfService: 0);
    }

    public void PublishFactsheet(AgvIdentity id, uint headerId, DateTimeOffset timestamp, string factsheetJson)
    {
        MqttFactsheet? mqttFactsheet;
        try
        {
            mqttFactsheet = JsonSerializer.Deserialize<MqttFactsheet>(factsheetJson, SystemTextJsonMessageSerializer.Options);
        }
        catch (JsonException exception)
        {
            return;
        }

        if (mqttFactsheet == null)
        {
            return;
        }

        mqttFactsheet.HeaderId = headerId;
        mqttFactsheet.Timestamp = timestamp;
        mqttFactsheet.SerialNumber = id.SerialNumber;
        Enqueue(id, Vda5050Specifications.Subtopics.Factsheet, mqttFactsheet, retain: false, qualityOfService: 0);
    }
    
    private void Enqueue<T>(AgvIdentity id, string subtopic, T dto, bool retain, int qualityOfService)
    {
        string topic = TopicParser.BuildTopic(_mqtt.InterfaceName, _mqtt.Version, id, subtopic);
        string payload = JsonSerializer.Serialize(dto, Vda5050Json.Options);
        _channel.Writer.TryWrite(new MqttSendMessage(topic, payload, retain, qualityOfService));
    }
}
