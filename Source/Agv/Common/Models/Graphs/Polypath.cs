using System.Numerics;
using Gebhardt.StoreWare.Agv.Common.Utilities;

namespace Gebhardt.StoreWare.Agv.Common.Models.Graphs;

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
        IEnumerable<(Vector3 First, Vector3 Second)> zipped = Points.SkipLast(1).Zip(Points.Skip(1));
        double length = 0;
        foreach (var (from, to) in zipped)
        {
            length += Mathematics.EuclideanDistance(from, to);
        }

        return length;
    }
}