using System.Collections.Immutable;
using JetBrains.Annotations;
using System.Numerics;
using Gebhardt.Acc.Common.Domain.Utilities;

namespace Gebhardt.Acc.Common.Domain.Models.Graphs;

/// <summary>
/// Contains helper functions for Bezier curves.
/// </summary>
[PublicAPI]
public class Bezier
{
    /// <summary>
    /// The control polygon of the Bezier curve.
    /// </summary>
    private IReadOnlyCollection<Vector2> ControlPolygon { get; }

    /// <summary>
    /// The degree of the Bezier curve.
    /// </summary>
    private int Degree => ControlPolygon.Count;

    /// <summary>
    /// Initializes a <see cref="Bezier"/> instance.
    /// </summary>
    /// <param name="controlPolygon">The control polygon of this Bézier curve.</param>
    public Bezier(ICollection<Vector2> controlPolygon)
    {
        if (controlPolygon.Count < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(controlPolygon), controlPolygon, "Bezier curve has to be at least degree 3.");
        }
        ControlPolygon = controlPolygon.ToImmutableList();
    }

    /// <summary>
    /// Interpolates the curve using <see cref="DeCasteljau(double)"/> algorithm.
    /// </summary>
    /// <param name="resolution">The amount of points returned, spaced equally on the curve (min. 2).</param>
    /// <exception cref="ArgumentOutOfRangeException">If the resolution is smaller than 2.</exception>
    public ICollection<Vector2> InterpolateCurve(int resolution)
    {
        if (resolution < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(resolution), resolution, "Resolution has to be at least 2.");
        }
        double stepSize = 1d / (resolution - 1);

        List<Vector2> result = new() { ControlPolygon.First() };

        for (int i = 1; i < resolution - 1; ++i)
        {
            result.Add(DeCasteljau(stepSize * i));
        }
        result.Add(ControlPolygon.Last());
        return result;
    }

    /// <summary>
    /// Performs the DeCasteljau algorithm on the provided Bezier curve.
    /// </summary>
    /// <param name="intervalPosition">The position on the interval [0,1] for which to calculate the point on the curve.</param>
    /// <exception cref="ArgumentOutOfRangeException">If the intervalPosition is outside of interval [0,1].</exception>
    public Vector2 DeCasteljau(double intervalPosition)
    {
        return DeCasteljau(intervalPosition, out _, out _);
    }

    /// <inheritdoc cref="DeCasteljau(double)"/>
    /// <param name="intervalPosition"></param>
    /// <param name="tangentPoint1">The first point that spans the tangent.</param>
    /// <param name="tangentPoint2">The second point that spans the tangent.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Vector2 DeCasteljau(double intervalPosition, out Vector2 tangentPoint1, out Vector2 tangentPoint2)
    {
        if (intervalPosition is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalPosition), intervalPosition, "Position must be inside the interval [0,1].");
        }

        var intermediatePoints = new Vector2[Degree, Degree];

        for (int i = 0; i < Degree; i++)
        {
            intermediatePoints[i, 0] = ControlPolygon.ElementAt(i);
        }

        for (int j = 1; j < Degree; j++)
        {
            for (int i = 0; i < Degree - j; i++)
            {
                intermediatePoints[i, j] = Vector2.Multiply(1 - (float)intervalPosition, intermediatePoints[i, j - 1])
                                           + Vector2.Multiply((float)intervalPosition, intermediatePoints[i + 1, j - 1]);
            }
        }

        tangentPoint1 = intermediatePoints[0, Degree - 2];
        tangentPoint2 = intermediatePoints[1, Degree - 2];

        return intermediatePoints[0, Degree - 1];
    }

    /// <summary>
    /// Calculates the length of this Bezier curve with the provided resolution.
    /// </summary>
    /// <param name="resolution">The resolution of the interpolation used to calculate the length.</param>
    public double GetLength(int resolution)
    {
        ICollection<Vector2> interpolatedCurve = InterpolateCurve(resolution);
        double length = 0;
        foreach (var (p1, p2) in interpolatedCurve.SkipLast(1).Zip(interpolatedCurve.Skip(1)))
        {
            length += Vector2.Distance(p1, p2);
        }
        return length;
    }

    /// <summary>
    /// Samples the curve at <paramref name="steps"/> equally spaced positions and returns the local frame
    /// (curve point, unit tangent and left-hand normal) at each position.
    /// </summary>
    /// <param name="steps">The amount of positions sampled along the curve (min. 2).</param>
    public (Vector2 Point, Vector2 Tangent, Vector2 Normal)[] GetSamples(int steps)
    {
        if (steps < 2)
        {
            throw new ArgumentException($"{nameof(steps)} has to be >=2");
        }

        double stepSize = 1d / (steps - 1);
        var frame = new (Vector2 Point, Vector2 Tangent, Vector2 Normal)[steps];
        var tangent = Vector2.UnitX;

        for (int i = 0; i < steps; i++)
        {
            double intervalPosition = Math.Clamp(stepSize * i, 0d, 1d);
            var curvePoint = DeCasteljau(intervalPosition, out var tangentPoint1, out var tangentPoint2);

            var tangentDirection = tangentPoint2 - tangentPoint1;
            if (tangentDirection.LengthSquared() > Mathematics.FloatComparator)
            {
                tangent = Vector2.Normalize(tangentDirection);
            }
            var normal = new Vector2(-tangent.Y, tangent.X);
            frame[i] = (curvePoint, tangent, normal);
        }

        return frame;
    }
}