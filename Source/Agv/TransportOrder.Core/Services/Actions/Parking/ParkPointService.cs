using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Nodes;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Parking;

/// <inheritdoc cref="IParkPointService" />
public class ParkPointService : BaseNodeReserve, IParkPointService
{
    /// <summary>
    /// Constructor for base charging point manager
    /// </summary>
    /// <param name="agvGraphRegistry">The graph registry</param>
    /// <param name="nodeStateRegistry"></param>
    /// <param name="routeService"></param>
    /// <param name="logger"></param>
    public ParkPointService(IAgvGraphRegistry agvGraphRegistry, INodeStateRegistry nodeStateRegistry, IRouteService routeService, ILogger<ParkPointService> logger) : base(agvGraphRegistry, nodeStateRegistry, routeService, logger)
    {

    }

    /// <inheritdoc />
    protected override string Description => "Park";

    /// <inheritdoc />
    protected override Expression<Func<GraphNode, bool>> NodeFilter => node => node.Type == AgvGraph.NodeType.Park;

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