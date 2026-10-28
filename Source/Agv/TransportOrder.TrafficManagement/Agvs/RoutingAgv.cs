using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Agvs;

/// <summary>
/// Representation of an Agv with relevant data for traffic management.
/// </summary>
internal class RoutingAgv(Guid agvId, AgvType agvType, RoutingNode? currentRoutingNode, bool isAutomatic, bool hasFatalError, bool isIdle)
{
    public Guid Id { get; } = agvId;

    public AgvType AgvType { get; } = agvType;

    public RoutingNode? CurrentRoutingNode { get; set; } = currentRoutingNode;

    public bool IsAutomatic { get; set; } = isAutomatic;

    public bool HasFatalError { get; set; } = hasFatalError;

    public bool IsIdle { get; set; } = isIdle;

    public LoadState? LoadState => CurrentRoutingNode?.LoadState;

    public override string ToString()
    {
        return $"AGV {Id}, CurrentRoutingNode={CurrentRoutingNode?.Description}, IsAutomatic={IsAutomatic}, HasFatalError={HasFatalError}, IsIdle={IsIdle}";
    }
}