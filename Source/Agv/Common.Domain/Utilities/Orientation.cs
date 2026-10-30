using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Utilities;

/// <summary>
/// Provides utility methods for comparing orientations and rotations, taking into account a specified tolerance for floating-point comparisons.
/// </summary>
[PublicAPI]
public static class Orientation
{
    private const double Tolerance = 1e-3;

    /// <summary>
    /// Determines whether two angles (in radians) are considered equal.
    /// </summary>
    public static bool Equals(double theta1, double theta2)
    {
        return Math.Abs(theta1 - theta2) < Tolerance;
    }

    /// <summary>
    /// Determines whether two nullable orientations (in radians) are considered equal.
    /// </summary>
    public static bool OrientationEquals(this double theta1, double theta2)
    {
        return Equals(theta1, theta2);
    }

    /// <summary>
    /// Determines whether two nullable orientations (in radians) are considered equal.
    /// </summary>
    public static bool OrientationEquals(this double? theta1, double? theta2)
    {
        if (theta1 is null || theta2 is null)
        {
            return theta1 is null && theta2 is null;
        }

        return Equals(theta1.Value, theta2.Value);
    }

    /// <summary>
    /// Determines whether two rotations (in radians) are considered equal.
    /// </summary>
    public static bool RotationEquals(this double alpha1, double alpha2)
    {
        return alpha1.OrientationEquals(alpha2);
    }

    /// <summary>
    /// Determines whether two nullable rotations (in radians) are considered equal.
    /// </summary>
    public static bool RotationEquals(this double? alpha1, double? alpha2)
    {
        return alpha1.OrientationEquals(alpha2);
    }
    
    /// <summary>
    /// Smallest absolute angle difference in radians (0..π), handles wrap-around at ±π.
    /// </summary>
    public static double GetOrientationDifference(double a, double b)
    {
        return Math.Abs(Math.IEEERemainder(a - b, 2 * Math.PI));
    }
}