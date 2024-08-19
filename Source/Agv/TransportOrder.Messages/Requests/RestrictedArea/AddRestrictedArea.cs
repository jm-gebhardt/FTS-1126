using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.RestrictedArea;

/// <summary>
/// Provides a request for adding a restricted area polygon.
/// </summary>
/// <param name="AreaPolygon">The restricted area polygon</param>
public record AddRestrictedArea(ICollection<PolygonPoint> AreaPolygon);