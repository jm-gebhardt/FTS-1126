using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Drawing;

/// <summary>
/// Command to delete the drawing layer for a map.
/// </summary>
/// <param name="CorrelationId">Correlation‑Id of the map.</param>
public record DeleteDrawingLayer(Guid CorrelationId) : CorrelatedBy<Guid>;
