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
public abstract class BaseNodeReservationManager : BaseNodePicker
{
    private readonly ISemaphoreProvider _semaphoreProvider;

    /// <inheritdoc cref="BaseNodeReservationManager"/>>
    protected BaseNodeReservationManager(
        IAgvDb agvDb,
        INodeStateRepository nodeStateRepository,
        IRouteService routeService,
        ILogger<BaseNodeReservationManager> logger,
        ISemaphoreProvider semaphoreProvider
    ) : base(nodeStateRepository, routeService, agvDb, logger)
    {
        _semaphoreProvider = semaphoreProvider;
    }

    /// <summary>
    /// Pick given node and reserve
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="startNodeId">The start node Id from which the route should be calculated. If null the last node Id of the agv will be used.</param>
    /// <returns></returns>
    protected async Task<GraphNode> PickAndReserveNodeAsync(Models.Agv.Agv agv, string? startNodeId = null)
    {
        return await _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var agvData = await AgvDb.Data.GetAgvDataAsync(agv.CorrelationId);
            var node = await PickNode(agv, agvData, startNodeId);

            await ReserveNodeAsync(node.Id, agv.CorrelationId);
            return node;
        });
    }

    ///  <inheritdoc cref="INodeStateRepository.ReserveNodeAsync"/>
    [UsedImplicitly]
    protected Task<bool> TryReserveNodeAsync(string nodeId)
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