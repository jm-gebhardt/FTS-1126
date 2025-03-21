using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Models.RestrictedArea;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Domain.Models.Zone;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;

/// <summary>
/// Provides extension methods for polygons
/// </summary>
public static class PolygonExtension
{
    /// <summary>
    /// True, if the given point is inside the polygon.
    /// </summary>
    /// <param name="polygon">The polygon.</param>
    /// <param name="x">The x coordinate of the point.</param>
    /// <param name="y">The y coordinate of the point.</param>
    public static bool PointIsInPolygon(this ICollection<PolygonPoint> polygon, float x, float y)
    {
        int polyCorners = polygon.Count;
        if (polyCorners == 0)
        {
            return false;
        }

        double[] polyY = polygon.Select(p => p.Y).ToArray();
        double[] polyX = polygon.Select(p => p.X).ToArray();
        return PolygonHelper.CheckPointInside(x, y, polyX, polyY, polyCorners);
    }

    /// <summary>
    /// True, if the given point is inside the polygon.
    /// </summary>
    /// <param name="polygon">The polygon.</param>
    /// <param name="x">The x coordinate of the point.</param>
    /// <param name="y">The y coordinate of the point.</param>
    public static bool PointIsInPolygon(this ICollection<TriggerPolygonPoint> polygon, float x, float y)
    {
        int polyCorners = polygon.Count;
        if (polyCorners == 0)
        {
            return false;
        }

        double[] polyY = polygon.Select(p => p.Y).ToArray();
        double[] polyX = polygon.Select(p => p.X).ToArray();
        return PolygonHelper.CheckPointInside(x, y, polyX, polyY, polyCorners);
    }

    /// <summary>
    /// True, if the given point is inside the polygon.
    /// </summary>
    /// <param name="polygon">The polygon.</param>
    /// <param name="x">The x coordinate of the point.</param>
    /// <param name="y">The y coordinate of the point.</param>
    public static bool PointIsInPolygon(this ICollection<RestrictedAreaPolygonPoint> polygon, float x, float y)
    {
        int polyCorners = polygon.Count;
        if (polyCorners == 0)
        {
            return false;
        }

        double[] polyY = polygon.Select(p => p.Y).ToArray();
        double[] polyX = polygon.Select(p => p.X).ToArray();
        return PolygonHelper.CheckPointInside(x, y, polyX, polyY, polyCorners);
    }

    /// <summary>
    /// True, if the given point is inside the polygon.
    /// </summary>
    /// <param name="polygon">The polygon.</param>
    /// <param name="x">The x coordinate of the point.</param>
    /// <param name="y">The y coordinate of the point.</param>
    public static bool PointIsInPolygon(this ICollection<ZonePolygonPoint> polygon, float x, float y)
    {
        int polyCorners = polygon.Count;
        if (polyCorners < 3)
        {
            return false;
        }

        double[] polyY = polygon.Select(p => p.Y).ToArray();
        double[] polyX = polygon.Select(p => p.X).ToArray();
        return PolygonHelper.CheckPointInside(x, y, polyX, polyY, polyCorners);
    }
}