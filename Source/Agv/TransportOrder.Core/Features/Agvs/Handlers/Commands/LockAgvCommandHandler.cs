using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Commands
{
    /// <summary>
    /// Defines a handler for the LockAgvRequest
    /// Gets agv from agvRegistry and Lock Agv to prevent it from being assigned to transport orders
    /// </summary>
    [UsedImplicitly]
    public class LockAgvCommandHandler : IConsumer<LockAgvCommand>
    {
        private readonly IAgvRegistry _agvRegistry;
        private readonly IBus _bus;

        /// <summary>
        /// <inheritdoc cref="LockAgvCommandHandler"/>
        /// </summary>
        /// <param name="agvRegistry"></param>
        /// <param name="bus"></param>
        public LockAgvCommandHandler(IAgvRegistry agvRegistry, IBus bus)
        {
            _agvRegistry = agvRegistry;
            _bus = bus;
        }

        /// <summary>
        /// <inheritdoc cref="LockAgvCommandHandler"/>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="AgvNotFoundException"></exception>
        public async Task Consume(ConsumeContext<LockAgvCommand> context)
        {
            var request = context.Message;
            var agv = _agvRegistry.GetRequiredAgv(request.CorrelationId);

            await _bus.Publish(new LockAgv(agv.CorrelationId));
            await context.RespondAsync(new LockAgvResult());
        }
    }

}