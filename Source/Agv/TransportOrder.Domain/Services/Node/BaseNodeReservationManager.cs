using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Node;

/// <summary>
/// Reserve Nodes
/// </summary>
public abstract class BaseNodeReservationManager : BaseNodePicker
{
    private readonly ISemaphoreProvider _semaphoreProvider;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="agvGraphRepository"></param>
    /// <param name="nodeStateRepository"></param>
    /// <param name="routeService"></param>
    /// <param name="logger"></param>
    /// <param name="semaphoreProvider"></param>
    /// <param name="agvDataRepository"></param>
    protected BaseNodeReservationManager(
        IAgvGraphRepository agvGraphRepository,
        INodeStateRepository nodeStateRepository,
        IRouteService routeService,
        ILogger<BaseNodeReservationManager> logger,
        ISemaphoreProvider semaphoreProvider,
        IAgvDataRepository agvDataRepository
    ) : base(agvGraphRepository, nodeStateRepository, routeService, logger, agvDataRepository)
    {
        _semaphoreProvider = semaphoreProvider;
    }

    /// <summary>
    /// Pick given node and reserve
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    protected async Task<GraphNode> PickAndReserveNodeAsync(Models.Agv.Agv agv)
    {
        return await _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var agvData = AgvDataRepository.GetAgvData(agv.CorrelationId);
            var node = await PickNode(agv, agvData);

            await ReserveNodeAsync(node.Id, agv.CorrelationId);
            return node;
        }, new CancellationToken());
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