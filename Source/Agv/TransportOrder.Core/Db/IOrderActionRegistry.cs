using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Db.Models;

// ReSharper disable UnusedMemberInSuper.Global
namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Db
{
    /// <summary>
    /// Abstracted interface for order action registry
    /// </summary>
    public interface IOrderActionRegistry : IDisposable
    {
        /// <summary>
        /// Insert action into database
        /// </summary><remarks>
        /// The sequence number is automatically incremented.
        /// </remarks>
        /// <param name="order">Order details</param>
        /// <param name="type">Action type</param>
        /// <param name="creationDateTime"></param>
        /// <param name="source">Source node</param>
        /// <param name="destination">Destination node</param>
        public void Insert(OrderDetails order, ActionType type, DateTime creationDateTime, string? source = null, string? destination = null);

        /// <summary>
        /// Get order action by key.
        /// </summary>
        /// <param name="id">Unique ID per action</param>
        /// <returns>Returns order action identified by key or null when not found</returns>
        public OrderAction? GetActionById(Guid id);

        /// <summary>
        /// Return list of all actions not started yet
        /// </summary>
        /// <param name="sortBySequenceAsc">If true, then pending actions get sorted according to underlying order's creation timestamp and then by its sequence number</param>
        /// <returns>Returns the list of pending actions, empty list when there are no pending actions</returns>
        public List<OrderAction> GetPendingActions(bool sortBySequenceAsc);

        /// <summary>
        /// Return list of all action belonging to the given order
        /// </summary>
        /// <param name="orderCorrelationId">Correlation ID of the order</param>
        /// <returns>Returns the list of actions, empty list when there are no actions</returns>
        public List<OrderAction> GetActionsOfOrder(Guid orderCorrelationId);

        /// <summary>
        /// Return the subsequent action belonging to the given order not started yet
        /// </summary>
        /// <param name="orderCorrelationId">Correlation ID of the order</param>
        /// <returns>Returns subsequent action or null when there are no more subsequent actions</returns>
        public OrderAction? GetSubsequentAction(Guid orderCorrelationId);

        /// <summary>
        /// Delete all actions belonging to the given order
        /// </summary>
        /// <param name="orderCorrelationId">Correlation ID of the order</param>
        public void DeleteOrder(Guid orderCorrelationId);

        /// <summary>
        /// Save changes
        /// </summary>
        public void SaveChanges();
    }
}
