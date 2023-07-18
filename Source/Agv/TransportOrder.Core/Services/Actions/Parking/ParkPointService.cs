using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Extensions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Nodes;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Parking;

/// <inheritdoc cref="IParkPointService" />
public class ParkPointService : BaseNodeReserve, IParkPointService
{
    /// <summary>
    /// Constructor for base charging point manager
    /// </summary>
    /// <param name="agvGraphRegistry">The graph registry</param>
    /// <param name="nodeStateRegistry"></param>
    public ParkPointService(IAgvGraphRegistry agvGraphRegistry, INodeStateRegistry nodeStateRegistry) : base(agvGraphRegistry, nodeStateRegistry)
    {
    }

    /// <inheritdoc />
    protected override string Description => "Park";

    /// <inheritdoc />
    protected override Expression<Func<GraphNode, bool>> NodeFilter => node => node.Type == AgvGraph.NodeType.Park;

    /// <inheritdoc />
    /// <summary>Primitive logic: shortest airline</summary>
    protected override Task<GraphNode> SelectBestNode(IEnumerable<GraphNode> availableNodes, IAgv agv)
    {
        // if last node is not set, just take base logic
        if (!agv.TryGetLastNodeId(out string lastNodeId))
        {
            return base.SelectBestNode(availableNodes, agv);
        }

        return GetNearestParkPoint(lastNodeId, agv, availableNodes);
    }

    private async Task<GraphNode> GetNearestParkPoint(string lastNodeId, IAgv agv, IEnumerable<GraphNode> availableNodes)
    {
        var lastNode = await GetNodeAsync(lastNodeId, agv);

        // according to the contract we get at least 1 node
        var nearestParkPoint = availableNodes.MinBy(n => n.DistanceTo(lastNode))!;
        return nearestParkPoint;
    }

    /// <inheritdoc />
    public Task<GraphNode> ReserveParkAsync(IAgv agv)
    {
        return PickAndReserveNodeAsync(agv);
    }

    /// <inheritdoc />
    public Task<bool> IsOnAParkNodeAsync(IAgv agv)
    {
        return IsAgvOnSuchANode(agv);
    }
}