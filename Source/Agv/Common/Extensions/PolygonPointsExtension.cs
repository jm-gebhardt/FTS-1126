using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Utilities;

namespace Gebhardt.Acc.Common.Extensions;

/// <summary>
/// Provides extension methods for polygons
/// </summary>
public static class PolygonPointsExtension
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
        if (polyCorners < 3)
        {
            return false;
        }

        double[] polyY = polygon.Select(p => p.Y).ToArray();
        double[] polyX = polygon.Select(p => p.X).ToArray();
        return PolygonHelper.CheckPointInside(x, y, polyX, polyY, polyCorners);
    }
}