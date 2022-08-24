using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Web.Models
{
    /// <summary>
    /// Order Model
    ///
    /// This is a simply copy of the <see cref="IOrder"/> to overcome a json serialization problems
    /// <see href="https://github.com/dotnet/runtime/issues/41749" />
    /// </summary>
    public record Order(string CurrentState, Guid? AssignedAgv, OrderDetails OrderDetails, Guid CorrelationId) : IOrder
    {
        /// <summary>
        /// Cast <see cref="IOrder"/> into <see cref="Order"/>
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public static Order FromIOrder(IOrder order)
        {
            return new Order(order.CurrentState, order.AssignedAgv, order.OrderDetails, order.CorrelationId);
        }
    }
}
