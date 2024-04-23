using System.Linq.Expressions;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.Node;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <inheritdoc cref="IChargingPointManager"/>
public class ChargingPointManager : BaseNodeReservationManager, IChargingPointManager
{
    /// <inheritdoc cref="ChargingPointManager"/>>
    public ChargingPointManager(IAgvDb agvDb, INodeStateRepository nodeStateRepository, IRouteService routeService, ILogger<ChargingPointManager> logger, ISemaphoreProvider<IChargingSemaphore> semaphoreProvider)
        : base(agvDb, nodeStateRepository, routeService, logger, semaphoreProvider)
    {
    }

    /// <inheritdoc />
    protected override string Description => "Charging";

    /// <inheritdoc />
    public async Task<GraphNode> ReserveChargerAsync(Models.Agv.Agv agv, string? startNodeId = null)
    {
        try
        {
            return await PickAndReserveNodeAsync(agv, startNodeId);
        }
        catch (Exception e) when (e is ReserveNodeFailedException or NoNodesAvailableException)
        {
            throw new NoChargeStationAvailableException();
        }
    }

    /// <inheritdoc />
    protected override Expression<Func<GraphNode, bool>> NodeFilter => node => node.Type == AgvGraph.NodeType.Charge;

    /// <inheritdoc />
    public async Task<GraphNode?> GetBestChargerAsync(Models.Agv.Agv agv, string? startNodeId = null)
    {
        try
        {
            var agvData = await AgvDb.Data.GetAgvDataAsync(agv.CorrelationId);
            return await PickNode(agv, agvData, startNodeId);
        }
        catch (NoNodesAvailableException)
        {
            return await Task.FromResult<GraphNode?>(null);
        }
    }

    /// <inheritdoc />
    public async Task ReserveChargerAsync(GraphNode node, Guid agvCorrelationId)
    {
        try
        {
            await ReserveNodeAsync(node.Id, agvCorrelationId);
        }
        catch (ReserveNodeFailedException)
        {
            throw new ChargeStationNotAvailableException(node.Id);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsOnAChargingNodeAsync(Models.Agv.Agv agv)
    {
        return await IsAgvOnSuchANode(agv);
    }

    /// <inheritdoc />
    public async Task<bool> AgvHasReservedChargerAsync(Guid agvCorrelationId)
    {
        List<string> nodes = (await AgvDb.Graphs.GetNodesAsync(agvCorrelationId, NodeFilter)).Select(n => n.Id).ToList();
        return await NodeStateRepository.IsAnyNodeReservedForAgv(agvCorrelationId, nodes);
    }
}