namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Drawing;

/// <summary>
/// Response containing the drawing layer data.
/// </summary>
/// <param name="MapCorrelationId">The correlation id of the map.</param>
/// <param name="Version">The version of the drawing layer for optimistic concurrency.</param>
/// <param name="ElementsJson">JSON serialized array of drawing elements.</param>
/// <param name="LastModified">Timestamp of the last modification.</param>
public record GotDrawingLayer(Guid? MapCorrelationId, int? Version, string? ElementsJson, DateTime? LastModified);
