using System.Numerics;
using Gebhardt.Acc.Common.Domain.Utilities;

namespace Gebhardt.Acc.Common.Domain.Models.Graphs;

/// <summary>
/// Represents a Polypath edge.
/// </summary>
public class Polypath
{
    /// <summary>
    /// The points on this Polypath.
    /// </summary>
    private List<Vector3> Points { get; }

    /// <summary>
    /// Initializes a <see cref="Polypath"/> instance.
    /// </summary>
    /// <param name="points">The points on the polypath.</param>
    public Polypath(List<Vector3> points)
    {
        Points = points;
    }

    /// <summary>
    /// Gets the length of his <see cref="Polypath"/>.
    /// </summary>
    /// <returns></returns>
    public double GetLength() {
        var zipped = Points.SkipLast(1).Zip(Points.Skip(1));
        double length = 0;
        foreach (var (from, to) in zipped)
        {
            length += Mathematics.EuclideanDistance(from, to);
        }

        return length;
    }
}