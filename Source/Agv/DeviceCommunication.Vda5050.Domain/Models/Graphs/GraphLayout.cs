using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;

/// <summary>
/// A versioned graph layout containing nodes, edges, and stations.
/// Represents a single layout shared across multiple vehicle types.
/// </summary>
[PublicAPI]
public record GraphLayout
{
    /// <summary>
    /// Unique identifier for this layout version.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The layout identifier (e.g. "layout-001"). Multiple versions may share the same LayoutId.
    /// </summary>
    public string LayoutId { get; init; } = string.Empty;

    /// <summary>
    /// The version of this layout.
    /// </summary>
    public string LayoutVersion { get; init; } = string.Empty;

    /// <summary>
    /// Optional human-readable name.
    /// </summary>
    public string? LayoutName { get; init; }

    /// <summary>
    /// Timestamp when this layout version was stored.
    /// </summary>
    public DateTime StoredAt { get; init; }

    /// <summary>
    /// Timestamp when this layout version was exported.
    /// </summary>
    public DateTime ExportedAt { get; init; }

    /// <summary>
    /// The nodes in this layout.
    /// </summary>
    public ICollection<GraphLayoutNode> Nodes { get; init; } = [];

    /// <summary>
    /// The edges in this layout.
    /// </summary>
    public ICollection<GraphLayoutEdge> Edges { get; init; } = [];

    /// <summary>
    /// The stations in this layout.
    /// </summary>
    public ICollection<GraphLayoutStation> Stations { get; init; } = [];
}