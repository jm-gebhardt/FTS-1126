using System.Linq.Expressions;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.Node;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

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
    protected override Expression<Func<GraphNode, bool>> NodeFilter => node => node.IsParker();

    /// <inheritdoc />
    public Task<GraphNode> ReserveParkAsync(Models.Agv.Agv agv)
    {
        return PickAndReserveNodeAsync(agv);
    }

    /// <inheritdoc />
    public Task<bool> HasParkersAsync(Guid agvCorrelationId)
    {
        return AgvDb.Graphs.HasAnyNodeOfTypeAsync(agvCorrelationId, AgvGraph.NodeType.Park);
    }

    /// <inheritdoc />
    public async Task ReserveParkAsync(Guid parkNodeId, Guid agvCorrelationId)
    {
        try
        {
            await ReserveNodeAsync(parkNodeId, agvCorrelationId);
        }
        catch (ReserveNodeFailedException)
        {
            throw new ParkNodeNotAvailableException(parkNodeId);
        }
    }

    /// <inheritdoc />
    public Task<bool> IsOnAParkNodeAsync(Models.Agv.Agv agv)
    {
        return IsAgvOnSuchANode(agv);
    }

    /// <inheritdoc />
    public async Task<bool> AgvHasReservedParkingNodeAsync(Guid agvCorrelationId)
    {
        List<Guid> nodes = (await AgvDb.Graphs.GetNodesAsync(agvCorrelationId, NodeFilter)).Select(n => n.Id).ToList();
        return await NodeStateRepository.IsAnyNodeReservedForAgv(agvCorrelationId, nodes);
    }
}