using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;

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
    }

    /// <summary>
    /// The type of the node
    /// </summary>
    public AgvGraph.NodeType Type { get; init; }

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
    /// The Theta coordinate of the node
    /// </summary>
    public float Theta { get; init; }

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
        if (Type != other.Type)
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
        if (obj.GetType() != this.GetType())
        {
            return false;
        }
        return Equals((GraphNode)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, (int)Type, X, Y, Z, Theta);
    }
}