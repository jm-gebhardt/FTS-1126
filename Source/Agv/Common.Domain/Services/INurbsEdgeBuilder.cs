using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Services;

/// <summary>
/// Provides an interface for building graph edges from NURBS curves.
/// </summary>
[PublicAPI]
public interface INurbsEdgeBuilder
{
    /// <summary>
    /// Build a graph edge from a NURBS curve.
    /// </summary>
    /// <param name="nurbs">The NURBS curve</param>
    /// <param name="fromNodeId">The edge start node id</param>
    /// <param name="toNodeId">The edge end node id</param>
    /// <param name="id">The edge id</param>
    /// <param name="timeStamp">The creation time.</param>
    /// <returns>A graph edge.</returns>
    GraphEdge BuildFromNurbs(Nurbs nurbs, Guid fromNodeId, Guid toNodeId, Guid? id = null, DateTime timeStamp = default);
}
