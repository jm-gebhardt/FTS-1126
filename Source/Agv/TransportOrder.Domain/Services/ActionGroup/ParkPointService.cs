using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;

/// <inheritdoc cref="IParkPointService" />
public class ParkPointService : BaseNodeReservationManager, IParkPointService
{
    /// <summary>
    /// Constructor for base charging point manager
    /// </summary>
    /// <param name="agvGraphRepository">The graph repository</param>
    /// <param name="nodeStateRepository"></param>
    /// <param name="routeService"></param>
    /// <param name="logger"></param>
    /// <param name="semaphoreProvider"></param>
    /// <param name="agvDataRepository"></param>
    public ParkPointService(IAgvGraphRepository agvGraphRepository, INodeStateRepository nodeStateRepository, IRouteService routeService, ILogger<ParkPointService> logger, IParkingSemaphoreProvider semaphoreProvider, IAgvDataRepository agvDataRepository) : base(agvGraphRepository, nodeStateRepository, routeService, logger, semaphoreProvider, agvDataRepository)
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