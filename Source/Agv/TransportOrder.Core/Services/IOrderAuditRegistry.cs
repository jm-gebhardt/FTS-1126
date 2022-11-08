using Gebhardt.StoreWare.Agv.TransportOrder.Core.Db.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services
{
    /// <summary>
    /// Abstracted interface for EfOrderAuditRegistry
    /// </summary>
    public interface IOrderAuditRegistry
    {
        /// <summary>
        /// Get all Entries found in EfOrderAuditRegistry
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        public IEnumerable<OrderChangedEvent> GetAllOrderChangedEventEntries();

        /// <summary>
        /// Checks if OrderChangedEvent related to its correlationId exists in registry
        /// </summary>
        /// <param name="correlationId">GUID of the entry looked for in the registry</param>
        [JetBrains.Annotations.UsedImplicitly]
        public bool Exists(Guid correlationId);

        /// <summary>
        /// Add a new OrderChangedEvent entry to the registry
        /// </summary>
        /// <param name="orderSaga"></param>
        /// <param name="eventName"></param>
        /// <param name="state"></param>
        /// <param name="hook"></param>
        /// <param name="cancellationToken"></param>
        public Task<bool> Add(OrderSaga orderSaga, string eventName, string state, Hook hook, CancellationToken cancellationToken);
    }
}
