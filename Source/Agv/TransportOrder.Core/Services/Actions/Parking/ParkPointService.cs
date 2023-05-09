using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Extensions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Nodes;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Parking;

/// <inheritdoc cref="IParkPointService" />
public class ParkPointService : BaseNodeReserve, IParkPointService
{
    /// <summary>
    /// Constructor for base charging point manager
    /// </summary>
    /// <param name="graphRegistry">The graph registry</param>
    /// <param name="nodeStateRegistry"></param>
    public ParkPointService(IGraphRegistry graphRegistry, INodeStateRegistry nodeStateRegistry) : base(graphRegistry, nodeStateRegistry)
    {
    }

    /// <inheritdoc />
    protected override string Description => "Park";

    /// <inheritdoc />
    protected override Expression<Func<Node, bool>> NodeFilter => node => node.Type == AgvGraph.NodeType.Park;

    /// <inheritdoc />
    /// <summary>Primitive logic: shortest airline</summary>
    protected override Node SelectBestNode(IEnumerable<Node> availableNodes, IAgv agv)
    {
        // if last node is not set, just take base logic
        if (!agv.TryGetLastNodeId(out string lastNodeId))
        {
            return base.SelectBestNode(availableNodes, agv);
        }

        var lastNode = GraphRegistry.GetNodes(n => n.Id == lastNodeId).SingleOrDefault();
        if (lastNode == null)
        {
            throw new NodeNotFoundException(agv, "lastNode was not found");
        }

        // according to the contract we get at least 1 node
        var nearestParkPoint = availableNodes.MinBy(n => n.DistanceTo(lastNode))!;
        return nearestParkPoint;
    }

    /// <inheritdoc />
    public Task<AgvGraph.Node> ReserveParkAsync(IAgv agv)
    {
        return PickAndReserveNodeAsync(agv);
    }

    /// <inheritdoc />
    public Task<bool> IsOnAParkNodeAsync(IAgv agv)
    {
        return Task.FromResult(IsAgvOnSuchANode(agv));
    }
}