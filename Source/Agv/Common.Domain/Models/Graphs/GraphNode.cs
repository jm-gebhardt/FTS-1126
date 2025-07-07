using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using static Gebhardt.Acc.Common.Domain.Models.Graphs.AgvGraph;

namespace Gebhardt.Acc.Common.Domain.Models.Graphs;

/// <summary>
/// Model for a node in the graph, it has X/Y/Z coordinates and a type.
/// </summary>
public class GraphNode : IEquatable<GraphNode>
{
    /// <summary>
    /// The Id of the node, based on the AGV vendor
    /// </summary><remarks>
    /// Usually this is the (numeric) ID passed by the AGV vendor.
    /// For internal reasons it can be necessary to build/combine an internal identification.
    /// </remarks>

    public string Id
    {
        get;
        [UsedImplicitly]
        init;
    } = null!;

    /// <summary>
    /// The time (UTC) when this node was created.
    /// </summary>
    public DateTime TimeStamp
    {
        get;
        [UsedImplicitly]
        set;
    }

    /// <summary>
    /// The type of the node
    /// </summary>
    [JsonConverter(typeof(JsonNodeTypeConverter))]
    public object Type
    {
        get => _type;
        init => _type = value;
    }

    private object _type = NodeType.WayPoint;

    /// <summary>
    /// The X coordinate of the node
    /// </summary>
    public float X { get; init; }

    /// <summary>
    /// The Y coordinate of the node
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    /// The Z coordinate of the node
    /// </summary>
    public float Z { get; init; }

    /// <summary>
    /// The drop lift height [mm], if set.
    /// </summary>
    public double? DropLiftHeight { get; set; }

    /// <summary>
    /// The pick lift height [mm], if set.
    /// </summary>
    public double? PickLiftHeight { get; set; }

    /// <summary>
    /// The docking direction of the node, if set.
    /// </summary>
    public DockingDirection? DockingDirection { get; set; }

    /// <summary>
    /// The Theta coordinate of the node
    /// </summary>
    public float Theta
    {
        get => _theta;
        init => _theta = value;
    }

    private float _theta;

    /// <summary>
    /// Simple string representation.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"{Id} [X={X}/Y={Y}/Z={Z}]";
    }

    /// <summary>
    /// Node Constructor
    /// </summary>
    /// <param name="id"></param>
    public GraphNode(string id)
    {
        Id = id;
    }

    /// <inheritdoc cref="GraphNode"/>>
    [JsonConstructor]
    public GraphNode()
    {
    }

    /// <inheritdoc cref="GraphNode"/>>
    public GraphNode(string id, float x, float y, float z, NodeType nodeType, double? pickLiftHeight, double? dropLiftHeight, DockingDirection? dockingDirection)
    {
        Id = id;
        Y = y;
        X = x;
        Z = z;
        Type = nodeType;
        PickLiftHeight = pickLiftHeight;
        DropLiftHeight = dropLiftHeight;
        DockingDirection = dockingDirection;
    }

    /// <inheritdoc cref="GraphNode"/>>
    public GraphNode(string id, float x, float y, float z, ICollection<NodeType> nodeType, double? pickLiftHeight, double? dropLiftHeight, DockingDirection? dockingDirection)
    {
        Id = id;
        Y = y;
        X = x;
        Z = z;
        Type = nodeType;
        PickLiftHeight = pickLiftHeight;
        DropLiftHeight = dropLiftHeight;
        DockingDirection = dockingDirection;
    }

    /// <summary>
    /// Update the node type.
    /// </summary>
    /// <param name="type">The new node type.</param>
    public void UpdateType(object type) => _type = type;

    /// <summary>
    /// Update the orientation of a node.
    /// </summary>
    /// <param name="theta">The new orientation.</param>
    public void UpdateOrientation(float theta) => _theta = theta;

    /// <inheritdoc />
    public bool Equals(GraphNode? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        if (Id != other.Id)
        {
            return false;
        }
        if (Type is NodeType type)
        {
            if (type != (NodeType) other.Type)
            {
                return false;
            }
        }
        else
        {
            var types = (ICollection<NodeType>) Type;
            var otherTypes = (ICollection<NodeType>) other.Type;
            foreach (var (thisType, otherType) in types.Zip(otherTypes))
            {
                if (thisType != otherType)
                {
                    return false;
                }
            }
        }

        if (!X.Equals(other.X))
        {
            return false;
        }
        if (!Y.Equals(other.Y))
        {
            return false;
        }
        if (!Z.Equals(other.Z))
        {
            return false;
        }
        if (!Theta.Equals(other.Theta))
        {
            return false;
        }
        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }
        if (ReferenceEquals(this, obj))
        {
            return true;
        }
        if (obj.GetType() != GetType())
        {
            return false;
        }
        return Equals((GraphNode) obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return HashCode.Combine(Id, Type, X, Y, Z, Theta);
    }
}

/// <summary>
/// Json converter for <see cref="NodeType"/>
/// </summary>
public class JsonNodeTypeConverter : JsonConverter<object>
{
    /// <inheritdoc cref="JsonConverter"/>>
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String when Enum.TryParse<NodeType>(reader.GetString(), true, out var nodeType):
                return nodeType;

            case JsonTokenType.String:
                throw new JsonException($"Unable to parse '{reader.GetString()}' as NodeType");

            case JsonTokenType.Number when reader.TryGetInt32(out int intValue):
            {
                if (Enum.IsDefined(typeof(NodeType), intValue))
                {
                    return (NodeType) intValue;
                }
                throw new JsonException($"Invalid numeric value {intValue} for NodeType");
            }

            case JsonTokenType.Number:
                throw new JsonException("Unable to parse number as NodeType");

            case JsonTokenType.StartArray:
            {
                var nodeTypes = new List<NodeType>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType == JsonTokenType.String && Enum.TryParse<NodeType>(reader.GetString(), true, out var function))
                    {
                        nodeTypes.Add(function);
                    }
                }
                return nodeTypes;
            }

            default:
                throw new JsonException($"Unexpected token {reader.TokenType}");
        }
    }

    /// <inheritdoc cref="JsonConverter"/>>
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case string nodeType:
                JsonSerializer.Serialize(writer, nodeType, options);
                break;

            case NodeType nodeType:
                JsonSerializer.Serialize(writer, nodeType.ToString(), options);
                break;

            case List<NodeType> nodeTypeList:
                List<string> nodeTypes = nodeTypeList.Select(n => n.ToString()).ToList();
                JsonSerializer.Serialize(writer, nodeTypes, options);
                break;

            default:
                throw new JsonException($"Unexpected value type {value.GetType()}");
        }
    }
}