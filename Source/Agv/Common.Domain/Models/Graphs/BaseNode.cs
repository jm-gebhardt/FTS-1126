using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models.Graphs;

/// <summary>
/// Base class for graph nodes, providing Id and Name.
/// </summary>
public class BaseNode
{
    /// <summary>
    /// The unique Id of the node.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The Name of the node.
    /// </summary>
    public string Name
    {
        get;
        [UsedImplicitly]
        init;
    } = null!;

    /// <inheritdoc cref="BaseNode"/>
    public BaseNode(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <inheritdoc cref="BaseNode"/>>
    [JsonConstructor]
    public BaseNode()
    {
    }
}