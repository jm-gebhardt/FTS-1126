using MassTransit;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Hubs;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{
    /// <summary>
    /// Handler class for <see cref="TransportOrderChanged"/> events
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class TransportOrderChangedConsumer : IConsumer<TransportOrderChanged>
    {
        private readonly IOrderHubContext _orderHubContext;

        /// <summary>
        /// Initializes a new instance of <see cref="TransportOrderChangedConsumer"/>
        /// </summary>
        public TransportOrderChangedConsumer(IOrderHubContext orderHubContext)
        {
            _orderHubContext = orderHubContext;
        }

        /// <summary>
        /// Handle <see cref="TransportOrderChanged"/> event
        /// </summary>
        public async Task Consume(ConsumeContext<TransportOrderChanged> context)
        {
            await _orderHubContext.SendOrderToAllClients(nameof(TransportOrderChanged), context.Message.Order, context.CancellationToken);
        }
    }
}
