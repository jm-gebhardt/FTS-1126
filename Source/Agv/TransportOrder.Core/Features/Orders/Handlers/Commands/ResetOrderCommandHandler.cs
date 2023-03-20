using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Configurations.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Handlers.Commands
{
    /// <summary>
    /// Defines a handler for the ResetOrderRequest
    /// Resets orders and starts a new transport
    /// </summary>
    /// <response code="200">Ok: Order was reset</response>
    /// <response code="400">BadRequest: Order is not completed yet</response>
    /// <response code="404">Not Found: Order with the given correlationId does not exist</response>
    [UsedImplicitly]
    public class ResetOrderCommandHandler : IConsumer<ResetOrderCommand>
    {
        private readonly IOrderRegistry _orderRegistry;
        private readonly IBus _bus;

        /// <summary>
        /// <inheritdoc cref="ResetOrderCommandHandler"/>
        /// </summary>
        /// <param name="orderRegistry"></param>
        /// <param name="bus"></param>
        public ResetOrderCommandHandler(IOrderRegistry orderRegistry, IBus bus)
        {
            _orderRegistry = orderRegistry;
            _bus = bus;
        }

        /// <summary>
        /// <inheritdoc cref="ResetOrderCommandHandler"/>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<ResetOrderCommand> context)
        {
            var request = context.Message;
            var order = _orderRegistry.GetOrder(request.CorrelationId);
            if (order == null)
            {
                throw new NotFoundException(nameof(order), nameof(request.CorrelationId), request.CorrelationId);
            }

            if (order.CurrentState != nameof(OrderStateMachine.OrderFailed) && order.CurrentState != nameof(OrderStateMachine.Final))
            {
                throw new NotInCorrectStateException("Order can not be reset because it is not completed yet");
            }

            await _bus.Publish(new ResetTransportOrder(order.CorrelationId));
            await context.RespondAsync(new ResetOrderResult());
        }
    }

}