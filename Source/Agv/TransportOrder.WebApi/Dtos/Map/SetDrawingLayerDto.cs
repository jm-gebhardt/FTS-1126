using System.ComponentModel.DataAnnotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;

/// <summary>
/// DTO for setting a drawing layer.
/// </summary>
/// <param name="ElementsJson">JSON serialized array of drawing elements.</param>
/// <param name="Version">Optional version for optimistic concurrency.</param>
public record SetDrawingLayerDto(
    [Required] string ElementsJson,
    int? Version = null
);
