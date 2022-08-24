using MassTransit;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Hubs;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{
    /// <summary>
    /// Observer for <see cref="OrderSaga"/>
    /// </summary>
    public class OrderStateObserver : IStateObserver<OrderSaga>
    {
        private readonly IOrderHubContext _orderHubContext;

        /// <summary>
        /// Initializes a new instance of <see cref="OrderStateObserver"/>
        /// </summary>
        public OrderStateObserver(IOrderHubContext orderHubContext)
        {
            _orderHubContext = orderHubContext;
        }

        /// <summary>
        /// Handle state change of an OrderSaga
        /// </summary>
        public async Task StateChanged(BehaviorContext<OrderSaga> context, State currentState, State previousState)
        {
            await _orderHubContext.SendOrderToAllClients("OrderStateChanged", context.Saga, context.CancellationToken);
        }
    }
}
