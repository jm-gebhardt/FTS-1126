using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.RestrictedArea;

/// <summary>
/// Provides a dto for a restricted area.
/// </summary>
[PublicAPI]
public record RestrictedAreaDto
{
    /// <summary>
    /// The id of the restricted area.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The restricted area polygon.
    /// </summary>
    public ICollection<PolygonPointDto> Area { get; set; } = [];
}