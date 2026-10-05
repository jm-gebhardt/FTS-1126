using Gebhardt.Acc.Common.Extensions;
using JetBrains.Annotations;
using MassTransit;
using System.Text.Json.Serialization;
using System.Text.Json;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;

/// <summary>
/// A list of all stations.
/// <para>See "station_list.schema".</para>
/// </summary>
[PublicAPI]
[EntityName("amq.topic")] // Specifies the RabbitMq exchange where the message is published
[RoutingKey("agv.stationList")] // Specifies the topic where subscribers can receive the message
public record MqttStationList
{
    /// <summary>
    /// Serial number of the AGV.
    /// </summary>
    public string SerialNumber { get; }

    /// <summary>
    /// The modification date of the station list. Timestamp in ISO8601 format.
    /// </summary>
    public DateTimeOffset ModificationDate { get; }

    /// <summary>
    /// The unique ID replied from the request.
    /// </summary>
    public Guid? RequestId { get; set; }

    /// <summary>
    /// Station list.
    /// </summary>
    public Station[] Stations { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="MqttStationList"/>
    /// </summary>
    /// <param name="serialNumber">Serial number of the AGV.</param>
    /// <param name="modificationDate">The modification date of the station list. Timestamp in ISO8601 format.</param>
    /// <param name="stations">Station list.</param>
    /// <param name="requestId">The unique ID replied from the request.</param>
    public MqttStationList(string serialNumber, DateTimeOffset modificationDate, Station[] stations,
        Guid? requestId = null)
    {
        SerialNumber = serialNumber;
        ModificationDate = modificationDate;
        Stations = stations;
        RequestId = requestId;
    }
}

/// <summary>
/// AGV station
/// </summary>
[PublicAPI]
public class Station
{
    /// <summary>
    /// The function of the station e.g. Pick/drop
    /// </summary>
    [JsonConverter(typeof(JsonFunctionConverter))]
    public object Function { get; set; } = null!;

    /// <summary>
    /// The unique identifier of the station
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// The position and rotation of the station based on to the maps/graphs coordinate system
    /// </summary>
    public Position Position { get; set; } = null!;

    /// <summary>
    /// Type identification
    /// </summary>
    public string TypeId { get; set; } = string.Empty;

    /// <inheritdoc cref="Station"/>>
    public Station(Function function, string identifier, Position position, string typeId)
    {
        Function = function;
        Identifier = identifier;
        Position = position;
        TypeId = typeId;
    }

    /// <inheritdoc cref="Station"/>>
    public Station(ICollection<Function> function, string identifier, Position position, string typeId)
    {
        Function = function;
        Identifier = identifier;
        Position = position;
        TypeId = typeId;
    }

    /// <inheritdoc cref="Station"/>>
    public Station()
    {
    }
}

/// <summary>
/// The position and rotation of the station based on to the maps/graphs coordinate system
/// </summary>
[PublicAPI]
public record Position
{
    /// <summary>
    /// Angle of AGV at that station
    /// </summary>
    [UsedImplicitly]
    public float Theta { get; set; }

    /// <summary>
    /// X coordinate
    /// </summary>
    public float X { get; }

    /// <summary>
    /// Y coordinate
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Build message.
    /// </summary>
    /// <param name="theta"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public Position(float theta, float x, float y)
    {
        Theta = theta;
        X = x;
        Y = y;
    }
}

/// <summary>
/// The function of the station e.g. Pick/drop
/// </summary>
public enum Function
{
    /// <summary>
    /// Pick and drop station
    /// </summary>
    [PublicAPI]
    Both,

    /// <summary>
    /// Charging station
    /// </summary>
    [PublicAPI]
    Charge,

    /// <summary>
    /// Drop only station
    /// </summary>
    [PublicAPI]
    Drop,

    /// <summary>
    /// Park position
    /// </summary>
    [PublicAPI]
    Park,

    /// <summary>
    /// Pick only station
    /// </summary>
    [PublicAPI]
    Pick,

    /// <summary>
    /// Maintenance station
    /// </summary>
    [PublicAPI]
    Maintenance
}

/// <summary>
/// Json converter for <see cref="Function"/>.
/// </summary>
public class JsonFunctionConverter : JsonConverter<object>
{
    /// <inheritdoc cref="JsonConverter"/>>
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            if (Enum.TryParse<Function>(reader.GetString(), true, out var function))
            {
                return function;
            }
            throw new JsonException($"Unable to parse '{reader.GetString()}' as Function");
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var functions = new List<Function>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String && Enum.TryParse<Function>(reader.GetString(), true, out var function))
                {
                    functions.Add(function);
                }
            }
            return functions;
        }
        throw new JsonException($"Unexpected token {reader.TokenType}");
    }

    /// <inheritdoc cref="JsonConverter"/>>
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case Function function:
                JsonSerializer.Serialize(writer, function.ToString(), options);
                break;

            case List<Function> functionList:
                var functions = functionList.Select(f => f.ToString()).ToList();
                JsonSerializer.Serialize(writer, functions, options);
                break;

            default:
                throw new JsonException($"Unexpected value type {value.GetType()}");
        }
    }
}
