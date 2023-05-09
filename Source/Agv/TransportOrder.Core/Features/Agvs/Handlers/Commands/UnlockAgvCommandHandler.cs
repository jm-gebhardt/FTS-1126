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
    /// Defines a handler for the SetIdle
    /// Gets the agv from agvRegistry and set status of agv to Idle
    /// </summary>
    [UsedImplicitly]
    public class UnlockAgvCommandHandler : IConsumer<UnlockAgvCommand>
    {
        private readonly IAgvRegistry _agvRegistry;
        private readonly IBus _bus;

        /// <summary>
        /// <inheritdoc cref="UnlockAgvCommandHandler"/>
        /// </summary>
        /// <param name="agvRegistry"></param>
        /// <param name="bus"></param>
        public UnlockAgvCommandHandler(IAgvRegistry agvRegistry, IBus bus)
        {
            _agvRegistry = agvRegistry;
            _bus = bus;
        }

        /// <summary>
        /// <inheritdoc cref="UnlockAgvCommandHandler"/>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="AgvNotFoundException"></exception>
        public async Task Consume(ConsumeContext<UnlockAgvCommand> context)
        {
            var request = context.Message;
            var agv = _agvRegistry.GetAgv(request.CorrelationId);
            if (agv == null)
            {
                throw new AgvNotFoundException(request.CorrelationId);
            }
            await _bus.Publish(new UnlockAgv(agv.CorrelationId));
            await context.RespondAsync(new UnlockAgvResult());
        }
    }

}