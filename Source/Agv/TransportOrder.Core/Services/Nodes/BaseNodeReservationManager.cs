using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Nodes
{
    /// <summary>
    /// Reserve Nodes
    /// </summary>
    public abstract class BaseNodeReservationManager : BaseNodePicker
    {
        private readonly ISemaphoreProvider _semaphoreProvider;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="agvGraphRegistry"></param>
        /// <param name="nodeStateRegistry"></param>
        /// <param name="routeService"></param>
        /// <param name="logger"></param>
        /// <param name="semaphoreProvider"></param>
        protected BaseNodeReservationManager(
            IAgvGraphRegistry agvGraphRegistry,
            INodeStateRegistry nodeStateRegistry,
            IRouteService routeService,
            ILogger<BaseNodeReservationManager> logger,
            ISemaphoreProvider semaphoreProvider
        ) : base(agvGraphRegistry, nodeStateRegistry, routeService, logger)
        {
            _semaphoreProvider = semaphoreProvider;
        }

        /// <summary>
        /// Pick given node and reserve
        /// </summary>
        /// <param name="agv"></param>
        /// <returns></returns>
        protected async Task<GraphNode> PickAndReserveNodeAsync(IAgv agv)
        {
            try
            {
                await _semaphoreProvider.Semaphore.WaitAsync();
                var node = await PickNode(agv);

                await ReserveNodeAsync(node.Id, agv.CorrelationId);
                return node;
            }
            finally
            {
                _semaphoreProvider.Semaphore.Release();
            }
        }

        ///  <inheritdoc cref="INodeStateRegistry.ReserveNodeAsync"/>
        [JetBrains.Annotations.UsedImplicitly]
        protected Task<bool> TryReserveNodeAsync(string nodeId)
        {
            return NodeStateRegistry.LockNodeAsync(nodeId);
        }

        ///  <inheritdoc cref="INodeStateRegistry.ReserveNodeAsync"/>
        protected async Task ReserveNodeAsync(string nodeId, Guid agvCorrelationId)
        {
            if (!await NodeStateRegistry.ReserveNodeAsync(nodeId, agvCorrelationId))
            {
                throw new ReserveNodeFailedException(nodeId);
            }
        }
    }
}