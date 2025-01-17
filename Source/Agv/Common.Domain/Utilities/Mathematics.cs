using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Utilities;

/// <summary>
/// A collection of useful mathematical functions
/// </summary>
public static class Mathematics
{
    /// <summary>
    /// Comparison value to check for double equality.
    /// </summary>
    public const double DoubleComparator = 0.000001;

    /// <summary>
    /// Comparison value to check for float equality
    /// </summary>
    public const float FloatComparator = (float)DoubleComparator;

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
    /// Calculates the orientation angle of a vector relative to the positive X-axis.
    /// </summary>
    /// <param name="start">The starting point of the vector.</param>
    /// <param name="end">The ending point of the vector.</param>
    /// <returns>
    /// The angle in radians between the vector and the positive X-axis, rotated 90 degrees clockwise.
    /// The result is in the range [-π, π].
    /// </returns>
    /// <remarks>
    /// - If both vectors are equal, the method returns π/2 (90 degrees).
    /// - If either vector is Zero, the method uses the other vector to calculate the tangent.
    /// - The angle is measured counterclockwise from the positive X-axis.
    /// - The result is adjusted by adding π/2 (90 degrees) to the calculated angle.
    /// </remarks>
    [PublicAPI]
    public static double OrientationToXAxis(Vector2 start, Vector2 end)
    {
        if (end == Vector2.Zero && start != Vector2.Zero)
        {
            // use tangent
            end -= start;
        }
        if (start == Vector2.Zero && end != Vector2.Zero)
        {
            // use tangent
            start -= end;
        }
        return (float)(Math.Atan2(end.Y - start.Y, end.X - start.X) + Math.PI / 2);
    }

    /// <summary>
    /// Calculates the euclidean distance between two <see cref="Gebhardt.Acc.Common.Domain.Models.Graphs.GraphNode"/>.
    /// </summary>
    /// <param name="first">The first node.</param>
    /// <param name="second">The second node.</param>
    public static double EuclideanDistance(GraphNode first, GraphNode second)
    {
        var vectorStart = new Vector3(first.X, first.Y, first.Z);
        var vectorEnd = new Vector3(second.X, second.Y, second.Z);

        return EuclideanDistance(vectorStart, vectorEnd);
    }

    /// <summary>
    /// Calculates the euclidean distance between two <see cref="Vector3"/>.
    /// </summary>
    /// <param name="first">The first vector.</param>
    /// <param name="second">The second vector.</param>
    public static double EuclideanDistance(Vector3 first, Vector3 second)
    {
        return (first - second).Length();
    }
    
    /// <summary>
    /// Rotates a <see cref="Vector3"/> projected to the plane and moves it a set distance.
    /// </summary>
    /// <param name="vector">^The <see cref="Vector3"/> to rotate and move.</param>
    /// <param name="angle">The (clockwise) angle to rotate the vector by in [rad].</param>
    /// <param name="distance">The distance to move the vector.</param>
    [PublicAPI]
    public static Vector3 RotateMoveVector3(Vector3 vector, double angle, double distance)
    {
        return new Vector3(vector.X - (float)(distance * Math.Sin(angle)), vector.Y - (float)(distance * Math.Cos(angle)), 0f);
    }
}