using Gebhardt.Acc.TransportOrder.Domain.Models.Map.Drawing;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Drawing;

/// <summary>
/// Response containing the drawing layer data.
/// </summary>
/// <param name="MapCorrelationId">The correlation id of the map.</param>
/// <param name="Version">The version of the drawing layer for optimistic concurrency.</param>
/// <param name="Elements">The drawing elements (rectangles, circles, lines, text).</param>
/// <param name="LastModified">Timestamp of the last modification.</param>
public record GotDrawingLayer(Guid? MapCorrelationId, int? Version, IList<DrawingElement>? Elements, DateTime? LastModified);
