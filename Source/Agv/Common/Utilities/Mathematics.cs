using System.Numerics;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;

namespace Gebhardt.StoreWare.Agv.Common.Utilities;

/// <summary>
/// A collection of useful mathematical functions
/// </summary>
public static class Mathematics
{
    /// <summary>
    /// Converts an angle in degrees to radians.
    /// </summary>
    /// <param name="angle">The angle in degrees</param>
    /// <returns></returns>
    public static double ConvertToRadians(double angle)
    {
        return Math.PI / 180 * angle;
    }

    /// <summary>
    /// Calculates the angle between two vectors in radians.
    /// </summary>
    /// <param name="v1">The first vector.</param>
    /// <param name="v2">The second vector.</param>
    public static double AngleBetween(Vector2 v1, Vector2 v2)
    {
        return Math.Acos(Vector2.Dot(v1, v2) / (Vector2.Distance(Vector2.Zero, v1) * Vector2.Distance(Vector2.Zero, v2)));
    }
    
    /// <summary>
    /// Calculates the euclidean distance between two <see cref="GraphNode"/>.
    /// </summary>
    /// <param name="first">The first node.</param>
    /// <param name="second">The second node.</param>
    public static double EuclideanDistance(GraphNode first, GraphNode second)
    {
        var vectorStart = new Vector3(first.X, first.Y, first.Z);
        var vectorEnd = new Vector3(second.X, second.Y, second.Z);

        return (vectorStart - vectorEnd).Length();
    }
}