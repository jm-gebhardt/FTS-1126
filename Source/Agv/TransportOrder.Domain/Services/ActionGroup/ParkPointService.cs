using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Microsoft.Extensions.Logging;
using TransportOrder.Domain.Repositories;
using TransportOrder.Domain.SemaphoreProvider;
using TransportOrder.Domain.Services.Node;
using TransportOrder.Domain.Services.Routing;

namespace TransportOrder.Domain.Services.ActionGroup;

/// <inheritdoc cref="IParkPointService" />
public class ParkPointService : BaseNodeReservationManager, IParkPointService
{
    /// <summary>
    /// Constructor for base charging point manager
    /// </summary>
    /// <param name="agvGraphRegistry">The graph registry</param>
    /// <param name="nodeStateRegistry"></param>
    /// <param name="routeService"></param>
    /// <param name="logger"></param>
    /// <param name="semaphoreProvider"></param>
    public ParkPointService(IAgvGraphRegistry agvGraphRegistry, INodeStateRegistry nodeStateRegistry, IRouteService routeService, ILogger<ParkPointService> logger, IParkingSemaphoreProvider semaphoreProvider) : base(agvGraphRegistry, nodeStateRegistry, routeService, logger, semaphoreProvider)
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