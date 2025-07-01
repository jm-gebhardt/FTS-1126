using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models.Graphs;

/// <summary>
/// The direction an agv can dock at a node.
/// </summary>
[PublicAPI]
public enum DockingDirection
{
    /// <summary>
    /// The agv needs to dock with the front.
    /// </summary>
    Front,

    /// <summary>
    /// The agv needs to dock with the back.
    /// </summary>
    Back,

    /// <summary>
    /// The agv can dock forward or backward.
    /// </summary>
    Both
}