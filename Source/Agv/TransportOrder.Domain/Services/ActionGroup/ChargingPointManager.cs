using System.Linq.Expressions;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
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
    public ChargingPointManager(IAgvDb agvDb, INodeRepository nodeRepository, IRouteService routeService, ILogger<ChargingPointManager> logger, ISemaphoreProvider<IChargingSemaphore> semaphoreProvider)
        : base(agvDb, nodeRepository, routeService, logger, semaphoreProvider)
    {
    }

    /// <inheritdoc />
    protected override string Description => "Charging";

    /// <inheritdoc />
    public async Task<GraphNode> ReserveChargerAsync(Agv agv, Guid? startNodeId = null, bool ignoreLocked = false, CancellationToken cancellationToken = default)
    {
        try
        {
            return await PickAndReserveNodeAsync(agv, startNodeId, ignoreLocked: ignoreLocked, cancellationToken: cancellationToken);
        }
        catch (Exception e) when (e is ReserveNodeFailedException or NoNodesAvailableException)
        {
            throw new NoChargeStationAvailableException();
        }
    }

    /// <inheritdoc />
    protected override Expression<Func<GraphNode, bool>> NodeFilter => node => node.IsCharger();

    /// <inheritdoc />
    public async Task<GraphNode?> GetBestChargerAsync(Agv agv, Guid? startNodeId = null)
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
    public async Task ReserveChargerAsync(GraphNode node, Guid agvCorrelationId, bool ignoreLocked=false, CancellationToken cancellationToken = default)
    {
        try
        {
            await ReserveNodeAsync(node.Id, agvCorrelationId, ignoreLocked, cancellationToken);
        }
        catch (ReserveNodeFailedException)
        {
            throw new ChargeStationNotAvailableException(node.Name);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsOnAChargingNodeAsync(Agv agv)
    {
        return await IsAgvOnSuchANode(agv);
    }

    /// <inheritdoc />
    public async Task<bool> AgvHasReservedChargerAsync(Guid agvCorrelationId)
    {
       return (await AgvDb.Graphs.GetNodesAsync(agvCorrelationId, NodeFilter)).Any(n => n.Lock != null && n.Lock.IsReservedForAgv(agvCorrelationId));
    }

    /// <inheritdoc />
    public async Task<GraphNode?> GetReservedChargerForAgvAsync(Guid agvCorrelationId)
    {
       return  (await AgvDb.Graphs.GetNodesAsync(agvCorrelationId, NodeFilter)).SingleOrDefault(n => n.Lock != null && n.Lock.IsReservedForAgv(agvCorrelationId));
    }
}