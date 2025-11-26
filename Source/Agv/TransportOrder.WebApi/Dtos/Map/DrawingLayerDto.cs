namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;

/// <summary>
/// DTO for a drawing layer.
/// </summary>
/// <param name="MapCorrelationId">The correlation id of the map.</param>
/// <param name="Version">The version of the drawing layer for optimistic concurrency.</param>
/// <param name="ElementsJson">JSON serialized array of drawing elements.</param>
/// <param name="LastModified">Timestamp of the last modification.</param>
public record DrawingLayerDto(Guid MapCorrelationId, int Version, string ElementsJson, DateTime LastModified);
