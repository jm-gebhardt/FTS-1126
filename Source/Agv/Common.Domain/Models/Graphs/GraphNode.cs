using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using static Gebhardt.Acc.Common.Domain.Models.Graphs.AgvGraph;

namespace Gebhardt.Acc.Common.Domain.Models.Graphs;

/// <summary>
/// Model for a node in the graph, it has X/Y/Z coordinates and a type.
/// </summary>
public class GraphNode : BaseNode, IEquatable<GraphNode>
{
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
    public ICollection<NodeType> Type
    {
        get => _type;
        init => _type = value;
    }

    private ICollection<NodeType> _type = [NodeType.WayPoint];

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
    /// The node lock.
    /// </summary>
    public NodeLock? Lock { get; set; }

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
        return $"{Name} [X={X}/Y={Y}/Z={Z}]";
    }

    /// <inheritdoc cref="GraphNode"/>>
    [JsonConstructor]
    public GraphNode()
    {
    }

    /// <inheritdoc cref="GraphNode"/>
    public GraphNode(Guid id, string name) : base(id, name)
    {
    }

    /// <inheritdoc cref="GraphNode"/>>
    public GraphNode(Guid id, string name, float x, float y, float z, NodeType nodeType, double? pickLiftHeight, double? dropLiftHeight, DockingDirection? dockingDirection) : this(id, name)
    {
        Y = y;
        X = x;
        Z = z;
        Type = [nodeType];
        PickLiftHeight = pickLiftHeight;
        DropLiftHeight = dropLiftHeight;
        DockingDirection = dockingDirection;
    }

    /// <inheritdoc cref="GraphNode"/>>
    public GraphNode(Guid id, string name, float x, float y, float z, ICollection<NodeType> nodeType, double? pickLiftHeight, double? dropLiftHeight, DockingDirection? dockingDirection) : this(id, name)
    {
        Y = y;
        X = x;
        Z = z;
        Type = nodeType;
        PickLiftHeight = pickLiftHeight;
        DropLiftHeight = dropLiftHeight;
        DockingDirection = dockingDirection;
    }

    /// <summary>
    /// String representation of the node type, used for DB filtering via computed column.
    /// </summary>
    public string TypeString
    {
        get => field ?? JsonSerializer.Serialize(Type.Select(t => t.ToString()));
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once RedundantAccessorBody
        private set => field = value;
    }

    /// <summary>
    /// Update the node type.
    /// </summary>
    /// <param name="types">The new node type(s).</param>
    public void UpdateType(ICollection<NodeType> types) => _type = types;

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
        if (Name != other.Name)
        {
            return false;
        }
        if (!TypesEqual(other))
        {
            return false;
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
        return HashCode.Combine(Id, Name, X, Y, Z, Theta);
    }

    private bool TypesEqual(GraphNode other) => Type.SequenceEqual(other.Type);
    }

/// <summary>
/// Json converter for <see cref="NodeType"/> collections. Handles legacy single-value JSON strings/numbers for backward compatibility.
/// </summary>
public class JsonNodeTypeConverter : JsonConverter<ICollection<NodeType>>
{
    /// <inheritdoc cref="JsonConverter"/>>
    public override ICollection<NodeType> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String when Enum.TryParse<NodeType>(reader.GetString(), true, out var nodeType):
                return [nodeType];

            case JsonTokenType.String:
                throw new JsonException($"Unable to parse '{reader.GetString()}' as NodeType");

            case JsonTokenType.Number when reader.TryGetInt32(out int intValue):
                if (Enum.IsDefined(typeof(NodeType), intValue))
                {
                    return [(NodeType) intValue];
                }
                throw new JsonException($"Invalid numeric value {intValue} for NodeType");

            case JsonTokenType.Number:
                throw new JsonException("Unable to parse number as NodeType");

            case JsonTokenType.StartArray:
            {
                var nodeTypes = new List<NodeType>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType == JsonTokenType.String && Enum.TryParse<NodeType>(reader.GetString(), true, out var nt))
                    {
                        nodeTypes.Add(nt);
                    }
                }
                return nodeTypes;
            }

            default:
                throw new JsonException($"Unexpected token {reader.TokenType}");
        }
    }

    /// <inheritdoc cref="JsonConverter"/>>
    public override void Write(Utf8JsonWriter writer, ICollection<NodeType> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Select(n => n.ToString()).ToList(), options);
    }
}