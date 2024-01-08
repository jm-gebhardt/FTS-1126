using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;

/// <inheritdoc cref="IParkPointService" />
public class ParkPointService : BaseNodeReservationManager, IParkPointService
{
    /// <inheritdoc cref="ParkPointService"/>>
    public ParkPointService(IAgvDb agvDb,
        INodeStateRepository nodeStateRepository,
        IRouteService routeService,
        ILogger<ParkPointService> logger,
        ISemaphoreProvider<IParkingSemaphore> semaphoreProvider) : base(agvDb,
        nodeStateRepository,
        routeService,
        logger,
        semaphoreProvider)
    {
    }

    /// <inheritdoc />
    protected override string Description => "Park";

    /// <inheritdoc />
    protected override Expression<Func<GraphNode, bool>> NodeFilter => node => node.Type == AgvGraph.NodeType.Park;

    /// <inheritdoc />
    public Task<GraphNode> ReserveParkAsync(Models.Agv.Agv agv)
    {
        return PickAndReserveNodeAsync(agv);
    }

    /// <inheritdoc />
    public Task<bool> IsOnAParkNodeAsync(Models.Agv.Agv agv)
    {
        return IsAgvOnSuchANode(agv);
    }
}