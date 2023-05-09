using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Nodes
{
    /// <summary>
    /// Reserve Nodes
    /// </summary>
    public abstract class BaseNodeReserve : BaseNodePicker
    {
        private readonly SemaphoreSlim _reserveSemaphoreSlim = new(1, 1);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="graphRegistry"></param>
        /// <param name="nodeStateRegistry"></param>
        protected BaseNodeReserve(
            IGraphRegistry graphRegistry,
            INodeStateRegistry nodeStateRegistry
        ) : base(graphRegistry, nodeStateRegistry)
        {
        }

        /// <summary>
        /// Pick given node and reserve
        /// </summary>
        /// <param name="agv"></param>
        /// <returns></returns>
        protected async Task<AgvGraph.Node> PickAndReserveNodeAsync(IAgv agv)
        {
            await _reserveSemaphoreSlim.WaitAsync();
            try
            {
                var node = PickNode(agv);

                await ReserveNodeAsync(node.Id, agv.CorrelationId);
                return node;
            }
            finally
            {
                _reserveSemaphoreSlim.Release();
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