using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
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
        INodeStateRepository nodeStateRepository,
        IRouteService routeService,
        ILogger<BaseNodeReservationManager> logger,
        ISemaphoreProvider semaphoreProvider
    ) : base(nodeStateRepository, routeService, agvDb, logger)
    {
        SemaphoreProvider = semaphoreProvider;
    }

    /// <summary>
    /// Pick given node and reserve
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="startNodeId">The start node Id from which the route should be calculated. If null the last node Id of the agv will be used.</param>
    /// <param name="reserveNode"></param>
    /// <param name="ignoredNodes">Nodes to ignore.</param>
    /// <returns></returns>
    protected async Task<GraphNode> PickAndReserveNodeAsync(Models.Agv.Agv agv, string? startNodeId = null, bool reserveNode = true, params string[] ignoredNodes)
    {
        return await SemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var agvData = await AgvDb.Data.GetAgvDataAsync(agv.CorrelationId);
            var node = await PickNode(agv, agvData, startNodeId, ignoredNodes);

            if(reserveNode)
            {
                await ReserveNodeAsync(node.Id, agv.CorrelationId);
            }
            return node;
        });
    }

    ///  <inheritdoc cref="INodeStateRepository.ReserveNodeAsync"/>
    [UsedImplicitly]
    protected Task<bool> TryLockNode(string nodeId)
    {
        return NodeStateRepository.LockNodeAsync(nodeId);
    }

    ///  <inheritdoc cref="INodeStateRepository.ReserveNodeAsync"/>
    protected async Task ReserveNodeAsync(string nodeId, Guid agvCorrelationId)
    {
        if (!await NodeStateRepository.ReserveNodeAsync(nodeId, agvCorrelationId))
        {
            throw new ReserveNodeFailedException(nodeId);
        }
    }
}