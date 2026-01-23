using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

/// <summary>
/// Provides a station generation helper for unit tests.
/// </summary>
public static class StationGenerator
{
    public static Station GetPickDropStation(string name, bool hasPalletJam = false, bool isLocked = false, NodeLockState nodeLockState = NodeLockState.Free, ICollection<Guid>? stationGroup = null)
    {
        var station = new Station { Id = name.ToGuid(), Name = name, PrimaryType = AgvGraph.NodeType.PickDrop, HasPalletJam = hasPalletJam, StationGroup = stationGroup ?? [] };
        var nodeLock = NodeLockGenerator.GenerateNodeLock(nodeName: name, nodeLockState: nodeLockState, isLocked: isLocked);
        station.NodeLock = nodeLock;
        if (isLocked)
        {
            station.Lock("user", null, null, null);
        }
        return station;
    }
}