using System.Text.Json;
using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;

/// <summary>
/// A graph as exported from Karis.
/// </summary>
[PublicAPI]
public class ExportedGraph
{
    /// <summary>
    /// Provides nodes.
    /// </summary>
    public ICollection<ExportedNode> Nodes { get; set; } = [];

    /// <summary>
    /// Provides edges.
    /// </summary>
    public ICollection<ExportedEdge> Edges { get; set; } = [];

    /// <summary>
    /// Provides stations.
    /// </summary>
    public ICollection<ExportedStation> Stations { get; set; } = [];
}

/// <summary>
/// A segment of the exported graph
/// </summary>
[PublicAPI]
public record ExportedEdge
{
    /// <summary>
    /// Control points.
    /// </summary>
    public ICollection<ExportedControlPoint> ControlPoints { get; set; } = [];

    /// <summary>
    /// The id of the edge.
    /// </summary>
    public string EdgeId { get; set; } = "";

    /// <summary>
    /// The id of the start node of the edge.
    /// </summary>
    public string StartId { get; set; } = "";

    /// <summary>
    /// The id of the end node of the edge.
    /// </summary>
    public string EndId { get; set; } = "";

    /// <summary>
    /// true/false. The edge can be traveled in both directions. If false, only from start to end
    /// is allowed.
    /// </summary>
    public bool Bidirectional { get; set; }

    /// <summary>
    /// The maximum allowed velocity on that edge
    /// </summary>
    public float MaxVelocity { get; set; }

    /// <summary>
    /// Edge type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EdgeType EdgeType { get; set; } = EdgeType.Direct;
}

/// <summary>
/// A control point in the exported segment.
/// </summary>
/// <param name="X">The x coordinate of the control point</param>
/// <param name="Y">The y coordinate of the control point</param>
[PublicAPI]
public record ExportedControlPoint(double X, double Y);

/// <summary>
/// A node as exported from Karis
/// </summary>
[PublicAPI]
public class ExportedNode
{
    /// <summary>
    /// The id of the node.
    /// </summary>
    public string NodeId { get; set; }

    /// <summary>
    /// The x-coordinate of the node.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// The y-coordinate of the node.
    /// </summary>
    public double Y { get; set; }

    /// <inheritdoc cref="ExportedNode"/>
    public ExportedNode(string nodeId, double x, double y)
    {
        NodeId = nodeId;
        X = x;
        Y = y;
    }
}

/// <summary>
/// A station as exported from Karis
/// </summary>
[PublicAPI]
public class ExportedStation
{
    /// <summary>
    /// The function of the station e.g. Pick/drop
    /// </summary>
    [JsonConverter(typeof(JsonFunctionConverter))]
    public object Function { get; set; } = null!;

    /// <summary>
    /// The unique identifier of the station
    /// </summary>
    public string Identifier { get; set; } = null!;

    /// <summary>
    /// The position and rotation of the station based on to the maps/graphs coordinate system
    /// </summary>
    public ExportedPosition Position { get; set; } = null!;

    /// <summary>
    /// Type identification
    /// </summary>
    public string TypeId { get; set; } = null!;

    /// <inheritdoc cref="ExportedStation"/>>
    [JsonConstructor]
    public ExportedStation()
    {
    }

    /// <summary>
    /// Build message.
    /// </summary>
    /// <param name="function"></param>
    /// <param name="identifier"></param>
    /// <param name="position"></param>
    /// <param name="typeId"></param>
    public ExportedStation(Function function, string identifier, ExportedPosition position, string typeId)
    {
        Function = function;
        Identifier = identifier;
        Position = position;
        TypeId = typeId;
    }

    /// <summary>
    /// Build message.
    /// </summary>
    /// <param name="function"></param>
    /// <param name="identifier"></param>
    /// <param name="position"></param>
    /// <param name="typeId"></param>
    public ExportedStation(ICollection<Function> function, string identifier, ExportedPosition position, string typeId)
    {
        Function = function;
        Identifier = identifier;
        Position = position;
        TypeId = typeId;
    }
}

/// <summary>
/// The position and rotation of the station based on to the maps/graphs coordinate system
/// </summary>
public record ExportedPosition
{
    /// <summary>
    /// Angle of AGV at that station
    /// </summary>
    [UsedImplicitly]
    public float Theta { get; }

    /// <summary>
    /// X coordinate
    /// </summary>
    public float X { get; }

    /// <summary>
    /// Y coordinate
    /// </summary>
    public float Y { get; }

    /// <inheritdoc cref="ExportedPosition"/>>
    public ExportedPosition(float theta, float x, float y)
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
                JsonSerializer.Serialize(writer, functionList, options);
                break;

            default:
                throw new JsonException($"Unexpected value type {value.GetType()}");
        }
    }
}