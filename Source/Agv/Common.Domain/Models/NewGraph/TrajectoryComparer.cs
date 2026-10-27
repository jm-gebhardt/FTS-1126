using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models.NewGraph;

/// <summary>
/// Compares trajectories by their curve. A missing trajectory equals one without control points.
/// </summary>
[PublicAPI]
public class TrajectoryComparer : IEqualityComparer<Trajectory?>
{
    /// <summary>
    /// The shared instance.
    /// </summary>
    public static TrajectoryComparer Instance { get; } = new();

    private static bool IsStraight(Trajectory? trajectory)
    {
        return trajectory is null || trajectory.ControlPoints.Count == 0;
    }

    /// <inheritdoc/>
    public bool Equals(Trajectory? x, Trajectory? y)
    {
        if (x is null || x.ControlPoints.Count == 0)
        {
            return IsStraight(y);
        }

        if (y is null || y.ControlPoints.Count == 0)
        {
            return false;
        }

        return x.Degree == y.Degree
               && x.KnotVector.SequenceEqual(y.KnotVector)
               && x.ControlPoints.SequenceEqual(y.ControlPoints);
    }

    /// <inheritdoc/>
    public int GetHashCode(Trajectory? obj)
    {
        if (obj is null || obj.ControlPoints.Count == 0)
        {
            return 0;
        }

        var hash = new HashCode();
        hash.Add(obj.Degree);
        foreach (double knot in obj.KnotVector)
        {
            hash.Add(knot);
        }

        foreach (var controlPoint in obj.ControlPoints)
        {
            hash.Add(controlPoint);
        }

        return hash.ToHashCode();
    }
}