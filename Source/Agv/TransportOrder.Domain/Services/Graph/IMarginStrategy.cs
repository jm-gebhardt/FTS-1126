using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Graph;

/// <summary>
/// Expands a polygon by a margin, e.g. a uniform safety clearance (20 cm in all directions).
/// </summary>
[PublicAPI]
public interface IMarginStrategy
{
    /// <summary>
    /// Returns a new polygon that extended the given polygon according to this strategy.
    /// </summary>
    Polygon Apply(Polygon polygon, MarginContext context);
}