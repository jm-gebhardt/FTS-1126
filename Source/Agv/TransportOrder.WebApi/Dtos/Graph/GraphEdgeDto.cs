using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Graph;

/// <summary>
/// Model for drawing an edge in the UI
/// </summary>
[PublicAPI]
public record GraphEdgeDto
{
    /// <summary>
    /// Node where the edge starts.
    /// </summary>
    public GraphNodeDto From { get; init; } = null!;

    /// <summary>
    /// Node where the edge ends.
    /// </summary>
    public GraphNodeDto To { get; init; } = null!;
}