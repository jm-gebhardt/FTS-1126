using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Handlers.Commands
{
    /// <summary>
    /// Completes order manually
    /// </summary>
    /// <response code="200">Ok: Order was completed</response>
    /// <response code="404">Not Found: Order with the given correlationId does not exist</response>
    [UsedImplicitly]
    public class CompleteOrderCommandHandler : IConsumer<CompleteOrderCommand>
    {
        private readonly IOrderRegistry _orderRegistry;
        private readonly IBus _bus;

        /// <summary>
        /// <inheritdoc cref="CompleteOrderCommandHandler"/>
        /// </summary>
        /// <param name="orderRegistry"></param>
        /// <param name="bus"></param>
        public CompleteOrderCommandHandler(IOrderRegistry orderRegistry, IBus bus)
        {
            _orderRegistry = orderRegistry;
            _bus = bus;
        }

        /// <summary>
        /// <inheritdoc cref="CompleteOrderCommandHandler"/>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<CompleteOrderCommand> context)
        {
            var request = context.Message;
            var order = _orderRegistry.GetOrder(request.CorrelationId);
            if (order == null)
            {
                throw new NotFoundException(nameof(order), nameof(request.CorrelationId), request.CorrelationId);
            }

            await _bus.Publish(new ManuallyCompleteTransportOrder(order.CorrelationId));
            await context.RespondAsync(new CompleteOrderResult());
        }
    }

}