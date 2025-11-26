using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Drawing;

/// <summary>
/// Command to set (or overwrite) the drawing layer for a map.
/// </summary>
/// <param name="CorrelationId">Correlation‑Id of the map.</param>
/// <param name="ElementsJson">JSON serialized array of drawing elements.</param>
/// <param name="Version">Optional version for optimistic concurrency.</param>
public record SetDrawingLayer(Guid CorrelationId, string ElementsJson, int? Version = null) : CorrelatedBy<Guid>;
