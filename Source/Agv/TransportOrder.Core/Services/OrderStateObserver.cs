using Gebhardt.StoreWare.Agv.TransportOrder.Core.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services
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
            await _orderHubContext.SendOrderToAllClients(nameof(Defines.HubMethods.OrderStateChanged), context.Saga, context.CancellationToken);
        }
    }
}
