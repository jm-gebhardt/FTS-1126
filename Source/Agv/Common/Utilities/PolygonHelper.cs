namespace Gebhardt.Acc.Common.Utilities;

/// <summary>
/// Provides helper methods for polygons
/// </summary>
public static class PolygonHelper
{
    /// <summary>
    /// Checks if a point (x,y) is inside a polygon.
    /// </summary>
    /// <param name="x">X-coordinate of the point.</param>
    /// <param name="y">Y-coordinate of the point.</param>
    /// <param name="polyX">The x-coordinates of the polygon points.</param>
    /// <param name="polyY">The y-coordinates of the polygon points.</param>
    /// <param name="polyCorners">The number of corners of the polygon.</param>
    /// <returns></returns>
    public static bool CheckPointInside(float x, float y, double[] polyX, double[] polyY, int polyCorners)
    {
        int j = polyCorners - 1;
        bool isInside = false;

        for (int i = 0; i < polyCorners; i++)
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