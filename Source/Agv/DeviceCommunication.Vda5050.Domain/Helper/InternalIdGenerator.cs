using Gebhardt.Acc.Common.Domain.Utilities;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Helper;

/// <summary>
/// Helper class to generate internal Ids for nodes, edges and stations based on lif information.
/// </summary>
public static class InternalIdGenerator
{
    /// <summary>
    /// Deterministically generates a layout Id from lif information.
    /// </summary>
    public static Guid GetLayoutId(string lifId, string layoutId)
    {
        return $"{lifId}.{layoutId}".ToGuid();
    }

    /// <summary>
    /// Deterministically generates a node Id from lif information.
    /// </summary>
    public static Guid GetNodeId(string lifId, string nodeId, out string name) => GetInternalId("N", lifId, nodeId, out name);

    /// <summary>
    /// Deterministically generates an edge Id from lif information.
    /// </summary>
    public static Guid GetEdgeId(string lifId, string edgeId, out string name) => GetInternalId("E", lifId, edgeId, out name);

    /// <summary>
    /// Deterministically generates a station Id from lif information.
    /// </summary>
    public static Guid GetStationId(string lifId, string stationId, out string name) => GetInternalId("S", lifId, stationId, out name);

    private static Guid GetInternalId(string typePrefix, string lifId, string entityId, out string name)
    {
        name = $"{lifId}.{entityId}";
        return $"{typePrefix}.{name}".ToGuid();
    }
}