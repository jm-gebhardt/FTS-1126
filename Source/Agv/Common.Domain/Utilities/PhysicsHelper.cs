using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Utilities;

/// <summary>
/// Contains helper functions for physics related problems.
/// </summary>
public static class PhysicsHelper
{
    /// <summary>
    /// Calculates the distance [m] to the maximum velocity for a given acceleration.
    /// </summary>
    /// <param name="acceleration">The acceleration [m/s^2].</param>
    /// <param name="maxVelocity">The maximum velocity [m/s].</param>
    /// <exception cref="ArgumentOutOfRangeException">If the acceleration is 0.</exception>
    [PublicAPI]
    public static double DistanceToVelocity(double acceleration, double maxVelocity)
    {
        return DistanceToVelocityFromVelocity(acceleration, maxVelocity, 0d);
    }

    /// <summary>
    /// Calculates the distance [m] to the maximum velocity for a given acceleration from a starting velocity.
    /// Can return a negative number if initialVelocity is higher than maxVelocity.
    /// </summary>
    /// <param name="acceleration">The acceleration [m/s^2].</param>
    /// <param name="maxVelocity">The maximum velocity [m/s].</param>
    /// <param name="initialVelocity">The initial velocity [m/s].</param>
    /// <exception cref="ArgumentOutOfRangeException">If the acceleration is 0.</exception>
    [PublicAPI]
    public static double DistanceToVelocityFromVelocity(double acceleration, double maxVelocity, double initialVelocity)
    {
        if (acceleration == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(acceleration), acceleration, "Acceleration can not be zero.");
        }

        return (Math.Pow(maxVelocity, 2d) - Math.Pow(initialVelocity, 2d)) / (2d * acceleration);
    }

    /// <summary>
    /// Calculates the time [s] to accelerate from a standing start over a specific distance.
    /// </summary>
    /// <param name="acceleration">The acceleration [m/s].</param>
    /// <param name="distance">The distance [m] to accelerate over.</param>
    /// <exception cref="ArgumentOutOfRangeException">If the acceleration is smaller than or equal to 0.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the distance is smaller than 0.</exception>
    [PublicAPI]
    public static double TimeToAccelerateFromStanding(double acceleration, double distance)
    {
        if (acceleration <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(acceleration), acceleration, "Acceleration can not be zero or smaller.");
        }
        if (distance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(distance), distance, "Distance can not be negative.");
        }

        return Math.Sqrt(2 * distance / acceleration);
    }

    /// <summary>
    /// Calculates the time [s] to accelerate from a velocity over a specific distance.
    /// </summary>
    /// <param name="acceleration">The acceleration [m/s^2].</param>
    /// <param name="distance">The distance [m] to accelerate over.</param>
    /// <param name="initialVelocity">The initial velocity [m/s].</param>
    /// <exception cref="ArgumentOutOfRangeException">If the acceleration is smaller than or equal to 0.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the distance is smaller than 0.</exception>
    [PublicAPI]
    public static double TimeToAccelerateFromVelocity(double acceleration, double distance, double initialVelocity)
    {
        if (acceleration <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(acceleration), acceleration, "Acceleration can not be zero or smaller.");
        }
        if (distance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(distance), distance, "Distance can not be negative.");
        }

        return Math.Sqrt(
            2 * distance / acceleration
            + Math.Pow(initialVelocity, 2) / Math.Pow(acceleration, 2)
        ) - initialVelocity / acceleration;
    }

    /// <summary>
    /// Calculates the velocity [m/s] after accelerating [m/s^2] over a specific distance [m].
    /// </summary>
    /// <param name="acceleration">The acceleration [m/s^2].</param>
    /// <param name="distance">The distance [m] to accelerate over.</param>
    /// <param name="initialVelocity">The initial velocity [m/s].</param>
    /// <exception cref="ArgumentOutOfRangeException">If the acceleration is smaller than 0.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the distance is smaller than 0.</exception>
    [PublicAPI]
    public static double VelocityAfterDistanceFromVelocity(double acceleration, double distance, double initialVelocity)
    {
        if (acceleration < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(acceleration), acceleration, "Acceleration can not be negative.");
        }
        if (distance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(distance), distance, "Distance can not be negative.");
        }

        return Math.Sqrt(Math.Pow(initialVelocity, 2) + 2 * acceleration * distance);
    }

    /// <summary>
    /// Calculates the time [s] to accelerate to and move at max velocity.
    /// </summary>
    /// <param name="acceleration">The acceleration [m/s^2].</param>
    /// <param name="distance">The distance [m].</param>
    /// <param name="maxVelocity">The maximum velocity [m/s].</param>
    /// <param name="initialVelocity">The initial velocity [m/s].</param>
    /// <exception cref="ArgumentOutOfRangeException">If the acceleration is smaller than or equal to 0.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the distance is smaller than 0.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the maxVelocity is 0.</exception>
    [PublicAPI]
    public static double CalculateTimeToAccelerateAndMoveAtMaxVelocity(double acceleration, double distance, double maxVelocity, double initialVelocity)
    {
        return CalculateTimeToAccelerateAndMoveAtMaxVelocity(acceleration, distance, maxVelocity, initialVelocity, out _);
    }

    /// <summary>
    /// Calculates the time [s] to accelerate to and move at max velocity and the final velocity [m/s] reached.
    /// Note: this is the max velocity if the distance is long enough to reach it.
    /// </summary>
    /// <param name="acceleration">The acceleration [m/s^2].</param>
    /// <param name="distance">The distance [m].</param>
    /// <param name="maxVelocity">The maximum velocity [m/s].</param>
    /// <param name="initialVelocity">The initial velocity [m/s].</param>
    /// <param name="finalVelocity">The final velocity after the distance has been traveled.</param>
    /// <exception cref="ArgumentOutOfRangeException">If the acceleration is smaller than or equal to 0.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the distance is smaller than 0.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the maxVelocity is 0.</exception>
    [PublicAPI]
    public static double CalculateTimeToAccelerateAndMoveAtMaxVelocity(double acceleration, double distance, double maxVelocity, double initialVelocity, out double finalVelocity)
    {
        if (maxVelocity == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxVelocity), maxVelocity, "Maximum velocity can not ne zero.");
        }

        double time = 0d;

        double distToMaxVelocity = DistanceToVelocityFromVelocity(acceleration, maxVelocity, initialVelocity);

        finalVelocity = maxVelocity;

        // We cannot accelerate to max velocity on the edge.
        if (distance < distToMaxVelocity)
        {
            time += TimeToAccelerateFromVelocity(acceleration, distance, initialVelocity);

            finalVelocity = VelocityAfterDistanceFromVelocity(acceleration, distance, initialVelocity);
        }
        else
        {
            // Accelerating to max velocity.
            time += TimeToAccelerateFromVelocity(acceleration, distToMaxVelocity, initialVelocity);

            // Moving at max velocity.
            time += (distance - distToMaxVelocity) / maxVelocity;
        }
        return time;
    }

    /// <summary>
    /// Calculates the lengths [m] of the three sections: <br/>
    ///     1.) Accelerate to max velocity <br/>
    ///     2.) Move at max velocity <br/>
    ///     3.) Decelerate to 0 (can be longer than total distance if the distance is not sufficient to brake.)<br/>
    /// </summary>
    /// <param name="acceleration">The acceleration [m/s^2]</param>
    /// <param name="distance">The total distance [m] to split into sections.</param>
    /// <param name="maxVelocity">The maximum velocity [m/s].</param>
    /// <param name="initialVelocity">The initial velocity [m/s].</param>
    /// <param name="accelerateSection">The section to accelerate on [m].</param>
    /// <param name="maxVelocitySection">The section to go at max velocity [m].</param>
    /// <param name="decelerateSection">The section to decelerate on [m].</param>
    /// <exception cref="ArgumentOutOfRangeException">If the acceleration is 0.</exception>
    [PublicAPI]
    public static void CalculateAccelerateMaxVelocityDecelerateSections(double acceleration, double distance, double maxVelocity, double initialVelocity, out double accelerateSection, out double maxVelocitySection, out double decelerateSection)
    {
        maxVelocitySection = 0;
        accelerateSection = DistanceToVelocityFromVelocity(acceleration, maxVelocity, initialVelocity);
        decelerateSection = DistanceToVelocity(acceleration, maxVelocity);

        double decelerateFromInitial = DistanceToVelocity(acceleration, initialVelocity);

        if (decelerateFromInitial > distance)
        {
            accelerateSection = 0;
            return;
        }

        if (decelerateSection + accelerateSection > distance)
        {
            double prevEdgeAcceleration = decelerateSection - accelerateSection;

            accelerateSection = (distance - prevEdgeAcceleration) / 2d;

            decelerateSection = distance - accelerateSection;

            accelerateSection = accelerateSection > 0 ? accelerateSection : 0;
            return;
        }

        maxVelocitySection = distance - accelerateSection - decelerateSection;

        maxVelocitySection = maxVelocitySection < 0 ? 0 : maxVelocitySection;
    }
}