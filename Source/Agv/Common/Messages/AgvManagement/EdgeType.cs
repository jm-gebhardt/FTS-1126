using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

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
    /// Bézier curve with two control points where both are the same.
    /// </summary>
    [UsedImplicitly]
    Bezier1,

    /// <summary>
    /// Bezier curve with two different control points.
    /// </summary>
    [UsedImplicitly]
    Bezier2,

    /// <summary>
    /// Path with up to ten points.
    /// </summary>
    [UsedImplicitly]
    Polypath
}