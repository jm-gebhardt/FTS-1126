using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Common.Services
{
    /// <summary>
    /// Interface for accessing the order registry
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public interface IOrderRegistry
    {
        /// <summary>
        /// Get count of all orders in the registry
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Get count of unassigned orders in the registry
        /// </summary>
        public int CountUnassigned { get; }

        /// <summary>
        /// Get list of all orders
        /// </summary>
        public IQueryable<IOrder> GetAllOrders();
        
        /// <summary>
        /// Get list of all open orders
        /// </summary>
        public IQueryable<IOrder> GetUnassignedOrders();

        /// <summary>
        /// Get specific order
        /// </summary>
        /// <returns>IOrder object or null if no order with the given correlationId was found</returns>
        public IOrder? GetOrder(Guid correlationId);
    }
}
