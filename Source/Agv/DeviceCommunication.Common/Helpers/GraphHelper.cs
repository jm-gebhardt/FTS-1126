using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;

namespace Gebhardt.Acc.DeviceCommunication.Common.Helpers;

/// <summary>
/// Helper class for graph related stuff.
/// </summary>
public static class GraphHelper
{
    /// <summary>
    /// Calculates Guid out of the given parameters.
    /// </summary>
    /// <param name="edgeType">The edge type.</param>
    /// <param name="fromNodeId">The source node of the edge.</param>
    /// <param name="metric">The edge metric.</param>
    /// <param name="toNodeId">The destination node of the edge.</param>
    /// <returns></returns>
    public static Guid ToGuid(EdgeType edgeType, string fromNodeId, float metric, string toNodeId)
    {
        return $"${edgeType}--{fromNodeId}-{metric}-{toNodeId}".ToGuid();
    }
}