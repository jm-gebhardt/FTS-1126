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

/// <inheritdoc cref="IParkPointService" />
public class ParkPointService : BaseNodeReservationManager, IParkPointService
{
    /// <inheritdoc cref="ParkPointService"/>>
    public ParkPointService(IAgvDb agvDb,
        INodeRepository nodeRepository,
        IRouteService routeService,
        ILogger<ParkPointService> logger,
        ISemaphoreProvider<IParkingSemaphore> semaphoreProvider) : base(agvDb,
        nodeRepository,
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
    public Task<GraphNode> ReserveParkAsync(Agv agv, bool ignoreLocked = false)
    {
        return PickAndReserveNodeAsync(agv, ignoreLocked:ignoreLocked);
    }

    /// <inheritdoc />
    public Task<bool> HasParkersAsync(Guid agvCorrelationId)
    {
        return AgvDb.Graphs.HasAnyNodeOfTypeAsync(agvCorrelationId, AgvGraph.NodeType.Park);
    }

    /// <inheritdoc />
    public async Task ReserveParkAsync(Guid parkNodeId, Guid agvCorrelationId, bool ignoreLocked = false, CancellationToken cancellationToken = default)
    {
        try
        {
            await ReserveNodeAsync(parkNodeId, agvCorrelationId, ignoreLocked, cancellationToken);
        }
        catch (ReserveNodeFailedException)
        {
            throw new ParkNodeNotAvailableException(parkNodeId);
        }
    }

    /// <inheritdoc />
    public Task<bool> IsOnAParkNodeAsync(Agv agv)
    {
        return IsAgvOnSuchANode(agv);
    }

    /// <inheritdoc />
    public async Task<bool> AgvHasReservedParkingNodeAsync(Guid agvCorrelationId)
    {
        return (await AgvDb.Graphs.GetNodesAsync(agvCorrelationId, NodeFilter)).Any(n => n.Lock != null && n.Lock.IsReservedForAgv(agvCorrelationId));
    }
}