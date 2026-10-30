using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;

/// <summary>
/// A rectangular area of the graph in [m], in graph coordinates.
/// </summary>
/// <param name="MinX">The smallest x of the area.</param>
/// <param name="MinY">The smallest y of the area.</param>
/// <param name="MaxX">The largest x of the area.</param>
/// <param name="MaxY">The largest y of the area.</param>
[PublicAPI]
public record ViewAreaDto(double MinX, double MinY, double MaxX, double MaxY) : IValidatableObject
{
    /// <inheritdoc/>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinX >= MaxX)
        {
            yield return new ValidationResult($"{nameof(MinX)} must be smaller than {nameof(MaxX)}.", [nameof(MinX), nameof(MaxX)]);
        }

        if (MinY >= MaxY)
        {
            yield return new ValidationResult($"{nameof(MinY)} must be smaller than {nameof(MaxY)}.", [nameof(MinY), nameof(MaxY)]);
        }
    }
}