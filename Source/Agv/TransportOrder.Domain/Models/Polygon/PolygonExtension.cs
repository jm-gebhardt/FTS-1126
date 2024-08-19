using Gebhardt.Acc.TransportOrder.Domain.Models.RestrictedArea;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;

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
        return CheckPointInside(x, y, polyX, polyY, polyCorners);
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
        return CheckPointInside(x, y, polyX, polyY, polyCorners);
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
        return CheckPointInside(x, y, polyX, polyY, polyCorners);
    }

    private static bool CheckPointInside(float x, float y, double[] polyX, double[] polyY, int polyCorners)
    {
        int i, j = polyCorners - 1;
        bool isInside = false;

        for (i = 0; i < polyCorners; i++)
        {
            bool isYCrossingEdge = (polyY[i] < y && polyY[j] >= y) || (polyY[j] < y && polyY[i] >= y);
            bool isXCrossingEdge = polyX[i] <= x || polyX[j] <= x;
            if (isYCrossingEdge && isXCrossingEdge)
            {
                bool isXRightOfCrossingPoint = polyX[i] + (y - polyY[i]) / (polyY[j] - polyY[i]) * (polyX[j] - polyX[i]) < x;
                if (isXRightOfCrossingPoint)
                {
                    isInside = !isInside;
                }
            }
            j = i;
        }

        return isInside;
    }
}