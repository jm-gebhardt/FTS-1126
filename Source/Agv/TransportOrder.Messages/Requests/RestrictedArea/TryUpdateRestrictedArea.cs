using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.RestrictedArea;

/// <summary>
/// Provides a request to try updating a restricted area.
/// </summary>
/// <param name="RestrictedAreaId">The id of the restricted area.</param>
/// <param name="UpdatedAreaPolygon">The updated area polygon.</param>
public record TryUpdateRestrictedArea(Guid RestrictedAreaId, ICollection<PolygonPoint> UpdatedAreaPolygon);