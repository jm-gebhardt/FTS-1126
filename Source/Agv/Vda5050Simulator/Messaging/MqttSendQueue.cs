using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Gebhardt.Acc.Vda5050Simulator.Configuration;
using Gebhardt.Acc.Vda5050Simulator.Domain;
using Gebhardt.Acc.Vda5050Simulator.Messaging.Dtos;
using Gebhardt.Acc.Vda5050Simulator.Runtime;
using Microsoft.Extensions.Options;

namespace Gebhardt.Acc.Vda5050Simulator.Messaging;

public sealed record MqttSendMessage(string Topic, string Payload, bool Retain, int QualityOfService);

/// <summary>
/// Maps domain snapshots to VDA5050 messages and queues them for the single publisher task of the gateway.
/// When the queue is full (e.g. broker unreachable), the oldest message is dropped.
/// </summary>
public sealed class MqttSendQueue(IOptions<MqttOptions> options) : IAgvPublisher
{
    private const int Capacity = 10_000;

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly MqttOptions _mqtt = options.Value;

    private readonly Channel<MqttSendMessage> _channel = Channel.CreateBounded<MqttSendMessage>(
        new BoundedChannelOptions(Capacity) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public ChannelReader<MqttSendMessage> Reader => _channel.Reader;

    public void PublishConnection(AgvId id, ConnectionState state, long headerId, DateTimeOffset timestamp)
    {
        var dto = new ConnectionMessageDto
        {
            HeaderId = headerId,
            Timestamp = FormatTimestamp(timestamp),
            Version = _mqtt.ProtocolVersion,
            Manufacturer = id.Manufacturer,
            SerialNumber = id.SerialNumber,
            ConnectionState = state.ToString().ToUpperInvariant()
        };
        Enqueue(id, "connection", dto, retain: true, qualityOfService: 1);
    }

    public void PublishState(AgvId id, AgvStateSnapshot snapshot)
    {
        var dto = new StateMessageDto
        {
            HeaderId = snapshot.HeaderId,
            Timestamp = FormatTimestamp(snapshot.Timestamp),
            Version = _mqtt.ProtocolVersion,
            Manufacturer = id.Manufacturer,
            SerialNumber = id.SerialNumber,
            OrderId = snapshot.OrderId,
            OrderUpdateId = snapshot.OrderUpdateId,
            LastNodeId = snapshot.LastNodeId,
            LastNodeSequenceId = snapshot.LastNodeSequenceId,
            Driving = snapshot.Driving,
            AgvPosition = snapshot.Position is { } p ? new AgvPositionDto(p.X, p.Y, p.Theta, p.MapId, p.PositionInitialized) : null,
            ActionStates = snapshot.ActionStates
                .Select(a => new ActionStateDto(a.ActionId, a.ActionType, a.Status.ToString().ToUpperInvariant()))
                .ToList(),
            BatteryState = new BatteryStateDto(snapshot.BatteryCharge, snapshot.Charging),
            Errors = snapshot.Errors
                .Select(e => new ErrorDto(
                    e.ErrorType,
                    e.Level.ToString().ToUpperInvariant(),
                    e.Description,
                    e.References.Select(r => new ErrorReferenceDto(r.Key, r.Value)).ToList()))
                .ToList(),
            NodeStates = snapshot.NodeStates.Select(n => new NodeStateDto(n.NodeId, n.SequenceId, n.Released)).ToList(),
            EdgeStates = snapshot.EdgeStates.Select(e => new EdgeStateDto(e.EdgeId, e.SequenceId, e.Released)).ToList()
        };
        Enqueue(id, "state", dto, retain: false, qualityOfService: 0);
    }

    private void Enqueue<T>(AgvId id, string subtopic, T dto, bool retain, int qualityOfService)
    {
        string topic = TopicParser.BuildTopic(_mqtt.InterfaceName, _mqtt.Version, id, subtopic);
        string payload = JsonSerializer.Serialize(dto, _jsonOptions);
        _channel.Writer.TryWrite(new MqttSendMessage(topic, payload, retain, qualityOfService));
    }

    private static string FormatTimestamp(DateTimeOffset timestamp)
        => timestamp.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", System.Globalization.CultureInfo.InvariantCulture);
}
