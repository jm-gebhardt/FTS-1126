namespace Gebhardt.Acc.Common.Domain.Models.Graphs;

/// <summary>
/// A 2D control point for a NURBS curve.
/// </summary>
/// <param name="X">The X coordinate.</param>
/// <param name="Y">The Y coordinate.</param>
/// <param name="Weight">
/// The rational weight. <c>null</c> is treated as 1.0 (non-rational).
/// Must not be zero.
/// </param>
public record NurbsControlPoint(double X, double Y, double? Weight = null);
