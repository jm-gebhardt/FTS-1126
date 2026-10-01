using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models.NewGraph;

/// <summary>
/// Trajectory described as a NURBS.
/// </summary>
[PublicAPI]
public record Trajectory
{
    private const int DefaultSampleCount = 50;

    private const double Tolerance = 1e-9;

    /// <inheritdoc cref="Trajectory"/>
    public Trajectory(IReadOnlyList<ControlPoint> controlPoints, double[] knotVector, long? degree = null)
    {
        ControlPoints = controlPoints;
        KnotVector = knotVector;
        Degree = degree;
    }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    public Trajectory()
    {
    }

    /// <summary>
    /// Control points defining the trajectory.
    /// </summary>
    public IReadOnlyList<ControlPoint> ControlPoints { get; init; } = new List<ControlPoint>();

    /// <summary>
    /// Degree of the trajectory curve. Default is 1. Range: [1 ... 3]
    /// </summary>
    public long? Degree { get; init; }

    /// <summary>
    /// Knot vector for the trajectory.
    /// </summary>
    public double[] KnotVector { get; init; } = [];

    /// <summary>
    /// Calculates the approximated length of the trajectory in meters.
    /// </summary>
    /// <param name="sampleCount">Number of sample points used for the approximation. Minimum is 2.</param>
    /// <returns>The approximated length of the trajectory.</returns>
    public double CalculateLength(int sampleCount = DefaultSampleCount)
    {
        IReadOnlyList<PolygonPoint> samples = Sample(sampleCount);

        double length = 0.0;
        for (int i = 1; i < samples.Count; i++)
        {
            double dx = samples[i].X - samples[i - 1].X;
            double dy = samples[i].Y - samples[i - 1].Y;
            length += Math.Sqrt(dx * dx + dy * dy);
        }

        return length;
    }

    /// <summary>
    /// Calculates the position on the trajectory at a given percentage of the traveled distance (arc length).
    /// </summary>
    /// <param name="percent">Percentage of the traveled distance along the trajectory. Range: [0 ... 100].</param>
    /// <param name="orientation">The orientation of the trajectory at the returned position, in radians, range [-PI ... PI].</param>
    /// <param name="sampleCount">Number of sample points used for the approximation. Minimum is 2.</param>
    /// <returns>The interpolated position on the trajectory at the given distance percentage.</returns>
    public PolygonPoint CalculatePositionAtDistancePercent(double percent, out double orientation, int sampleCount = DefaultSampleCount)
    {
        percent = Math.Clamp(percent, 0.0, 100.0);

        IReadOnlyList<PolygonPoint> samples = Sample(sampleCount);
        if (samples.Count == 0)
        {
            orientation = 0.0;

            return new PolygonPoint(0.0, 0.0);
        }

        if (samples.Count == 1)
        {
            orientation = 0.0;

            return samples[0];
        }

        double[] cumulativeLength = new double[samples.Count];
        for (int i = 1; i < samples.Count; i++)
        {
            double dx = samples[i].X - samples[i - 1].X;
            double dy = samples[i].Y - samples[i - 1].Y;
            cumulativeLength[i] = cumulativeLength[i - 1] + Math.Sqrt(dx * dx + dy * dy);
        }

        double totalLength = cumulativeLength[^1];
        if (totalLength <= Tolerance)
        {
            orientation = 0.0;

            return samples[0];
        }

        double targetDistance = totalLength * percent / 100.0;

        for (int i = 1; i < samples.Count; i++)
        {
            if (targetDistance > cumulativeLength[i] && i != samples.Count - 1)
            {
                continue;
            }

            double segmentLength = cumulativeLength[i] - cumulativeLength[i - 1];
            double t = segmentLength > Tolerance ? (targetDistance - cumulativeLength[i - 1]) / segmentLength : 0.0;

            double x = samples[i - 1].X + (samples[i].X - samples[i - 1].X) * t;
            double y = samples[i - 1].Y + (samples[i].Y - samples[i - 1].Y) * t;

            orientation = GetTangentAngle(samples, i - 1, i);

            return new PolygonPoint(x, y);
        }

        orientation = GetTangentAngle(samples, samples.Count - 2, samples.Count - 1);

        return samples[^1];
    }

    /// <summary>
    /// Calculates the tangent angle between two sample points, falling back to neighboring samples if the
    /// segment is degenerate.
    /// </summary>
    private static double GetTangentAngle(IReadOnlyList<PolygonPoint> samples, int fromIndex, int toIndex)
    {
        var from = new ControlPoint(samples[fromIndex].X, samples[fromIndex].Y);
        var to = new ControlPoint(samples[toIndex].X, samples[toIndex].Y);
        if (TryGetAngle(from, to, out double angle))
        {
            return angle;
        }

        for (int i = toIndex + 1; i < samples.Count; i++)
        {
            var candidate = new ControlPoint(samples[i].X, samples[i].Y);
            if (TryGetAngle(from, candidate, out angle))
            {
                return angle;
            }
        }

        for (int i = fromIndex - 1; i >= 0; i--)
        {
            var candidate = new ControlPoint(samples[i].X, samples[i].Y);
            if (TryGetAngle(candidate, to, out angle))
            {
                return angle;
            }
        }

        return 0.0;
    }

    /// <summary>
    /// Calculates the orientation of the trajectory at its end point.
    /// </summary>
    /// <remarks>
    /// The curve is tangential to the vector between the last two control points.
    /// </remarks>
    /// <returns>The end angle in radians, range [-PI ... PI].</returns>
    public double CalculateEndAngle()
    {
        IList<ControlPoint> controlPoints = ControlPoints as IList<ControlPoint> ?? [.. ControlPoints];

        int end = controlPoints.Count - 1;
        for (int i = end - 1; i >= 0; i--)
        {
            if (TryGetAngle(controlPoints[i], controlPoints[end], out double angle))
            {
                return angle;
            }
        }

        return 0.0;
    }

    /// <summary>
    /// Calculates the orientation of the trajectory at its start point.
    /// </summary>
    /// <remarks>
    /// The curve is tangential to the vector between the first two control points.
    /// </remarks>
    /// <returns>The start angle in radians, range [-PI ... PI].</returns>
    public double CalculateStartAngle()
    {
        IList<ControlPoint> controlPoints = ControlPoints as IList<ControlPoint> ?? [.. ControlPoints];

        for (int i = 1; i < controlPoints.Count; i++)
        {
            if (TryGetAngle(controlPoints[0], controlPoints[i], out double angle))
            {
                return angle;
            }
        }

        return 0.0;
    }

    /// <summary>
    /// Samples the trajectory with an equidistant parameter distribution.
    /// </summary>
    /// <param name="sampleCount">Number of sample points to calculate. Minimum is 2.</param>
    /// <returns>The sampled points including the start and the end point of the trajectory.</returns>
    public IReadOnlyList<PolygonPoint> Sample(int sampleCount = DefaultSampleCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sampleCount, 2);

        IList<ControlPoint> controlPoints = ControlPoints as IList<ControlPoint> ?? [.. ControlPoints];
        if (controlPoints.Count == 0)
        {
            return [];
        }

        if (controlPoints.Count == 1)
        {
            return [new PolygonPoint(controlPoints[0].X, controlPoints[0].Y)];
        }

        int degree = GetEffectiveDegree(controlPoints.Count);
        double[] knots = GetEffectiveKnotVector(controlPoints.Count, degree);

        double start = knots[degree];
        double end = knots[controlPoints.Count];

        // Buffers for the iterative basis function evaluation, reused for every sample.
        Span<double> basisValues = new double[degree + 1];
        Span<double> left = new double[degree + 1];
        Span<double> right = new double[degree + 1];

        var samples = new List<PolygonPoint>(sampleCount);
        for (int i = 0; i < sampleCount; i++)
        {
            double t = start + (end - start) * i / (sampleCount - 1);
            samples.Add(Evaluate(controlPoints, knots, degree, t, basisValues, left, right));
        }

        return samples;
    }

    /// <summary>
    /// Calculates the <paramref name="degree"/> + 1 non-zero basis functions of the given knot span
    /// using the iterative Cox-de-Boor recurrence.
    /// </summary>
    private static void CalculateBasisValues(
        double[] knots,
        int span,
        int degree,
        double t,
        Span<double> basisValues,
        Span<double> left,
        Span<double> right)
    {
        basisValues[0] = 1.0;

        for (int j = 1; j <= degree; j++)
        {
            left[j] = t - knots[span + 1 - j];
            right[j] = knots[span + j] - t;

            double saved = 0.0;
            for (int r = 0; r < j; r++)
            {
                double denominator = right[r + 1] + left[j - r];
                if (denominator <= Tolerance)
                {
                    basisValues[r] = saved;
                    saved = 0.0;

                    continue;
                }

                double temp = basisValues[r] / denominator;
                basisValues[r] = saved + right[r + 1] * temp;
                saved = left[j - r] * temp;
            }

            basisValues[j] = saved;
        }
    }

    /// <summary>
    /// Determines the index of the knot span containing <paramref name="t"/>.
    /// </summary>
    private static int FindSpan(double[] knots, int controlPointCount, int degree, double t)
    {
        int last = controlPointCount - 1;
        if (t >= knots[last + 1])
        {
            return last;
        }

        if (t <= knots[degree])
        {
            return degree;
        }

        int low = degree;
        int high = last + 1;
        int mid = (low + high) / 2;
        while (t < knots[mid] || t >= knots[mid + 1])
        {
            if (t < knots[mid])
            {
                high = mid;
            }
            else
            {
                low = mid;
            }

            mid = (low + high) / 2;
        }

        return mid;
    }

    private static bool TryGetAngle(ControlPoint from, ControlPoint to, out double angle)
    {
        double dx = to.X - from.X;
        double dy = to.Y - from.Y;
        if (Math.Abs(dx) <= Tolerance && Math.Abs(dy) <= Tolerance)
        {
            angle = 0.0;

            return false;
        }

        angle = Math.Atan2(dy, dx);

        return true;
    }

    private static PolygonPoint Evaluate(
        IList<ControlPoint> controlPoints,
        double[] knots,
        int degree,
        double t,
        Span<double> basisValues,
        Span<double> left,
        Span<double> right)
    {
        int span = FindSpan(knots, controlPoints.Count, degree, t);
        CalculateBasisValues(knots, span, degree, t, basisValues, left, right);

        double x = 0.0;
        double y = 0.0;
        double weightSum = 0.0;

        // Only the degree + 1 control points of the current span contribute to the curve point.
        for (int i = 0; i <= degree; i++)
        {
            var controlPoint = controlPoints[span - degree + i];
            double weight = controlPoint.Weight ?? 1.0;
            double factor = basisValues[i] * weight;

            x += factor * controlPoint.X;
            y += factor * controlPoint.Y;
            weightSum += factor;
        }

        if (Math.Abs(weightSum) < Tolerance)
        {
            return new PolygonPoint(controlPoints[^1].X, controlPoints[^1].Y);
        }

        return new PolygonPoint(x / weightSum, y / weightSum);
    }

    private int GetEffectiveDegree(int controlPointCount)
    {
        int degree = Degree is > 0 ? (int)Degree.Value : 1;

        return Math.Min(degree, controlPointCount - 1);
    }

    private double[] GetEffectiveKnotVector(int controlPointCount, int degree)
    {
        int expectedLength = controlPointCount + degree + 1;
        if (KnotVector.Length == expectedLength)
        {
            return KnotVector;
        }

        // Fall back to a clamped uniform knot vector if the given knot vector does not match the curve definition.
        double[] knots = new double[expectedLength];
        int innerCount = controlPointCount - degree;
        for (int i = 0; i < expectedLength; i++)
        {
            if (i <= degree)
            {
                knots[i] = 0.0;
            }
            else if (i >= controlPointCount)
            {
                knots[i] = 1.0;
            }
            else
            {
                knots[i] = (double)(i - degree) / innerCount;
            }
        }

        return knots;
    }
}