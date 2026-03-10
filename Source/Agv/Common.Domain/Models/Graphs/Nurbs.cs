using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.Common.Domain.Models.Graphs;

/// <summary>
///  Non-Uniform Rational B-Spline (NURBS).
/// </summary>
public class Nurbs
{
    /// <summary>
    /// The control points (including start and end of the curve).
    /// </summary>
    public NurbsControlPoint[] ControlPoints { get; init; } = [];

    /// <summary>
    /// The knot vector.
    /// </summary>
    public double[] KnotVector { get; init; } = [];

    /// <summary>
    /// The degree of the spline.
    /// </summary>
    public int Degree { get; init; }

    /// <summary>
    /// Returns <c>true</c> if any control point carries an explicit weight (rational NURBS).
    /// </summary>
    public bool IsWeighted => ControlPoints.Any(cp => cp.Weight.HasValue);

    /// <summary>
    /// Convert control points to flat list [CP1_x, CP1_y, CP2_x, CP2_y,...].
    /// </summary>
    public List<double> ControlPointsAsList()
    {
        var cPoints = new List<double>(ControlPoints.Length * 2);
        foreach (var cp in ControlPoints)
        {
            cPoints.Add(cp.X);
            cPoints.Add(cp.Y);
        }
        return cPoints;
    }

    /// <summary>
    /// Convert control points to homogeneous list [w*CP1_x, w*CP1_y, w1, w*CP2_x, w*CP2_y, w2,...].
    /// <c>null</c> weights default to 1.0.
    /// </summary>
    /// <exception cref="NurbsParsingException">If any weight is zero.</exception>
    public List<double> ControlPointsAsHomogeneousList()
    {
        var cPoints = new List<double>(ControlPoints.Length * 3);
        foreach (var cp in ControlPoints)
        {
            double w = cp.Weight ?? 1.0;
            if (w == 0.0)
            {
                throw new NurbsParsingException("NURBS control point weight must not be zero.");
            }
            cPoints.Add(w * cp.X);
            cPoints.Add(w * cp.Y);
            cPoints.Add(w);
        }
        return cPoints;
    }
}
