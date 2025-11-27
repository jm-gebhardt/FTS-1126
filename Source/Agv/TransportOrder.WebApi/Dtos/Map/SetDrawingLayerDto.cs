using System.ComponentModel.DataAnnotations;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map.Drawing;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;

/// <summary>
/// DTO for setting a drawing layer.
/// </summary>
/// <param name="Elements">The drawing elements (rectangles, circles, lines, text).</param>
/// <param name="Version">Optional version for optimistic concurrency.</param>
public record SetDrawingLayerDto(
    [Required] IList<DrawingElement> Elements,
    int? Version = null
);
