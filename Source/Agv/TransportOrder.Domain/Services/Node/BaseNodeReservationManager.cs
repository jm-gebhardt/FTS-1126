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
    /// <returns></returns>
    protected async Task<GraphNode> PickAndReserveNodeAsync(Agv agv, Guid? startNodeId = null, bool reserveNode = true, bool ignoreLocked = false, params Guid[] ignoredNodes)
    {
        return await SemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var agvData = await AgvDb.Data.GetAgvDataAsync(agv.CorrelationId);
            var node = await PickNode(agv, agvData, startNodeId, ignoreLocked, ignoredNodes);

            if (reserveNode)
            {
                return await ReserveNodeAsync(node.Id, agv.CorrelationId, ignoreLocked);
            }

            return node;
        });
    }

    /// <summary>
    /// Reserve the given node for the given agv.
    /// </summary>
    /// <param name="nodeId">The node id.</param>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <param name="ignoreLocked">Flag to reserve node even if it is locked.</param>
    /// <returns>The updated reserved node.</returns>
    /// <exception cref="ReserveNodeFailedException">Thrown, in case node reservation failed.</exception>
    protected async Task<GraphNode> ReserveNodeAsync(Guid nodeId, Guid agvCorrelationId, bool ignoreLocked = false)
    {
        try
        {
            var node = await NodeRepository.GetNodeAsync(nodeId);
            if (!node.Reserve(agvCorrelationId, ignoreLocked))
            {
                throw new ReserveNodeFailedException(nodeId);
            }

            if (!await NodeRepository.UpdateLock(node, ignoreLocked))
            {
                throw new ReserveNodeFailedException(nodeId);
            }

            return node;
        }
        catch (Exception)
        {
            throw new ReserveNodeFailedException(nodeId);
        }
    }
}