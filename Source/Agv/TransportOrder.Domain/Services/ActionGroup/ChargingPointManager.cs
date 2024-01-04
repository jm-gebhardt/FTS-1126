using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Services;
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
    /// <param name="graphRepository">The graph repository.</param>
    /// <param name="nodeStateRepository"></param>
    /// <param name="routeService"></param>
    /// <param name="logger"></param>
    /// <param name="semaphoreProvider"></param>
    /// <param name="agvDataRepository"></param>
    public ChargingPointManager(IGraphRepository graphRepository, INodeStateRepository nodeStateRepository, IRouteService routeService, ILogger<ChargingPointManager> logger, ISemaphoreProvider<IChargingSemaphore> semaphoreProvider, IAgvDataRepository agvDataRepository)
        : base(graphRepository, nodeStateRepository, routeService, logger, semaphoreProvider, agvDataRepository)
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
            var agvData = await AgvDataRepository.GetAgvDataAsync(agv.CorrelationId);
            return await PickNode(agv, agvData);
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