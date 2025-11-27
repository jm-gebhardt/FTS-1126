using Gebhardt.Acc.TransportOrder.Domain.Models.Map.Drawing;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Map;

/// <summary>
/// Represents a drawing layer for a map containing user-drawn vector elements.
/// </summary>
/// <remarks>
/// The drawing layer allows users to add visual annotations (rectangles, circles, lines, text)
/// to the map background for better visual reference.
/// </remarks>
public record MapDrawingLayer
{
    /// <summary>
    /// The unique identifier of the drawing layer.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The correlation id of the map this drawing layer belongs to.
    /// </summary>
    public Guid MapCorrelationId { get; init; }

    /// <summary>
    /// Version number for optimistic concurrency control.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// The drawing elements (rectangles, circles, lines, text).
    /// Stored as a JSON column in the database using EF Core 10 polymorphic JSON support.
    /// </summary>
    public IList<DrawingElement> Elements { get; set; } = [];

    /// <summary>
    /// Timestamp of the last modification.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// EF Core constructor.
    /// </summary>
    [UsedImplicitly]
    private MapDrawingLayer()
    {
    }

    /// <inheritdoc cref="MapDrawingLayer"/>
    /// <param name="mapCorrelationId">The correlation id of the map.</param>
    public MapDrawingLayer(Guid mapCorrelationId)
    {
        MapCorrelationId = mapCorrelationId;
    }

    /// <inheritdoc cref="MapDrawingLayer"/>
    /// <param name="mapCorrelationId">The correlation id of the map.</param>
    /// <param name="elements">The drawing elements.</param>
    public MapDrawingLayer(Guid mapCorrelationId, IList<DrawingElement> elements)
    {
        MapCorrelationId = mapCorrelationId;
        Elements = elements;
    }
}
