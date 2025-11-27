using Gebhardt.Acc.TransportOrder.Domain.Models.Map.Drawing;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Drawing;

/// <summary>
/// Command to set (or overwrite) the drawing layer for a map.
/// </summary>
/// <param name="CorrelationId">Correlation‑Id of the map.</param>
/// <param name="Elements">The drawing elements (rectangles, circles, lines, text).</param>
/// <param name="Version">Optional version for optimistic concurrency.</param>
public record SetDrawingLayer(Guid CorrelationId, IList<DrawingElement> Elements, int? Version = null) : CorrelatedBy<Guid>;
