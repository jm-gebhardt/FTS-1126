using Gebhardt.Acc.Common.Utilities;

namespace Gebhardt.Acc.DeviceCommunication.Helpers;

/// <summary>
/// Helper class for graph related stuff.
/// </summary>
public static class GraphHelper
{
    /// <summary>
    /// Calculates Guid out of the given parameters.
    /// </summary>
    /// <param name="fromNodeId"></param>
    /// <param name="metric"></param>
    /// <param name="toNodeId"></param>
    /// <returns></returns>
    public static Guid ToGuid(string fromNodeId, float metric, string toNodeId)
    {
        return $"{fromNodeId}-{metric}-{toNodeId}".ToGuid();
    }
}