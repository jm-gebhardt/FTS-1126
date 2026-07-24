using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Node;

/// <summary>
/// Reserve Nodes
/// </summary>
[PublicAPI]
public abstract class BaseNodeReservationManager : BaseNodePicker
{
    /// <summary>
    /// The semaphore for reserving nodes.
    /// </summary>
    protected readonly ISemaphoreProvider SemaphoreProvider;

    /// <inheritdoc cref="BaseNodeReservationManager"/>>
    protected BaseNodeReservationManager(
        IAgvDb agvDb,
        INodeRepository nodeRepository,
        IRouteService routeService,
        ILogger<BaseNodeReservationManager> logger,
        ISemaphoreProvider semaphoreProvider
    ) : base(nodeRepository, routeService, agvDb, logger)
    {
        SemaphoreProvider = semaphoreProvider;
    }

    /// <summary>
    /// Pick given node and reserve
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="startNodeId">The start node Id from which the route should be calculated. If null the last node Id of the agv will be used.</param>
    /// <param name="reserveNode"></param>
    /// <param name="ignoreLocked">Flag to reserve node even if it is locked.</param>
    /// <param name="ignoredNodes">Nodes to ignore.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    protected async Task<GraphNode> PickAndReserveNodeAsync(Agv agv, Guid? startNodeId = null, bool reserveNode = true, bool ignoreLocked = false, CancellationToken cancellationToken = default, params Guid[] ignoredNodes)
    {
        return await SemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var agvData = await AgvDb.Data.GetAgvDataAsync(agv.CorrelationId, cancellationToken);
            var node = await PickNode(agv, agvData, startNodeId, ignoreLocked, ignoredNodes);

            if (reserveNode)
            {
                await ReserveNodeAsync(node.Id, agv.CorrelationId, ignoreLocked, cancellationToken);
            }
            return node;
        }, cancellationToken);
    }

    /// <summary>
    /// Reserve the given node for the given agv.
    /// </summary>
    /// <param name="nodeId">The node id.</param>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <param name="ignoreLocked">Flag to reserve node even if it is locked.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="ReserveNodeFailedException">Thrown, in case node reservation failed.</exception>
    protected async Task ReserveNodeAsync(Guid nodeId, Guid agvCorrelationId, bool ignoreLocked = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var node = await NodeRepository.GetNodeAsync(nodeId, cancellationToken);
            if (!node.Reserve(agvCorrelationId, ignoreLocked))
            {
                throw new ReserveNodeFailedException(nodeId);
            }

            if (!await NodeRepository.UpdateLock(node, ignoreLocked))
            {
                throw new ReserveNodeFailedException(nodeId);
            }
        }
        catch (Exception)
        {
            throw new ReserveNodeFailedException(nodeId);
        }
    }
}