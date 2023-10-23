using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;

/// <inheritdoc cref="IChargingPointManager"/>
public class ChargingPointManager : BaseNodeReservationManager, IChargingPointManager
{
    /// <summary>
    /// Constructor for base charging point manager
    /// </summary>
    /// <param name="graphRegistry">The graph registry</param>
    /// <param name="nodeStateRegistry"></param>
    /// <param name="routeService"></param>
    /// <param name="logger"></param>
    /// <param name="semaphoreProvider"></param>
    public ChargingPointManager(IAgvGraphRegistry graphRegistry, INodeStateRegistry nodeStateRegistry, IRouteService routeService, ILogger<ChargingPointManager> logger, IChargingSemaphoreProvider semaphoreProvider)
        : base(graphRegistry, nodeStateRegistry, routeService, logger, semaphoreProvider)
    {
    }

    /// <inheritdoc />
    protected override string Description => "Charging";

    /// <inheritdoc />
    public Task<GraphNode> ReserveChargerAsync(Models.Agv.Agv agv)
    {
        return PickAndReserveNodeAsync(agv);
    }

    /// <inheritdoc />
    protected override Expression<Func<GraphNode, bool>> NodeFilter => node => node.Type == AgvGraph.NodeType.Charge;

    /// <inheritdoc />
    public async Task<GraphNode?> GetBestChargerAsync(Models.Agv.Agv agv)
    {
        try
        {
            return await PickNode(agv);
        }
        catch (NoNodesAvailableException)
        {
            return await Task.FromResult<GraphNode?>(null);
        }
    }

    /// <inheritdoc />
    public Task ReserveChargerAsync(GraphNode node, Guid agvCorrelationId)
    {
        return ReserveNodeAsync(node.Id, agvCorrelationId);
    }

    /// <inheritdoc />
    public async Task<bool> IsOnAChargingNodeAsync(Models.Agv.Agv agv)
    {
        return await IsAgvOnSuchANode(agv);
    }
}