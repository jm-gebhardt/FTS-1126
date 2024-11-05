using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;

/// <summary>
/// A dto for the location of a trigger.
/// </summary>
[PublicAPI]
public record TriggerLocationDto : LocationDto
{
    /// <summary>
    /// The id of the location.
    /// </summary>
    public Guid Id { get; set; }

    /// <inheritdoc cref="TriggerLocationDto"/>>
    public TriggerLocationDto()
    {
        Id = Guid.NewGuid();
        Polygon = [];
    }
}