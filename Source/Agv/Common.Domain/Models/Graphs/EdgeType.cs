using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models.Graphs;

/// <summary>
/// Marks the different types that an <see cref="GraphEdge"/> can have.
/// </summary>
public enum EdgeType
{
    /// <summary>
    /// Direct connection between the start and end point.
    /// </summary>
    Direct,

    /// <summary>
    /// Exits/Enters the nodes at an angle.
    /// </summary>
    Slanted,

    /// <summary>
    /// Bézier curve of 2nd grade with three control points (including start and end).
    /// </summary>
    [UsedImplicitly]
    Bezier2,

    /// <summary>
    /// Bézier curve of 3rd grade with four control points (including start and end).
    /// </summary>
    [UsedImplicitly]
    Bezier3,

    /// <summary>
    /// Bézier curve of 5th grade with six control points (including start and end).
    /// </summary>
    [UsedImplicitly]
    Bezier5,

    /// <summary>
    /// Path with up to ten points.
    /// </summary>
    [UsedImplicitly]
    Polypath
}