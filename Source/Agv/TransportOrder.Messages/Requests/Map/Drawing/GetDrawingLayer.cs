using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Drawing;

/// <summary>
/// Request to get the drawing layer for a map.
/// </summary>
/// <param name="CorrelationId">Correlation‑Id of the map.</param>
public record GetDrawingLayer(Guid CorrelationId) : CorrelatedBy<Guid>;
