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
    /// Defines a handler for the <see cref="LockAgvCommand"/> and <see cref="UnlockAgvCommand"/>
    /// </summary>
    [UsedImplicitly]
    public class LockAndUnlockAgvCommandHandler : IConsumer<LockAgvCommand>, IConsumer<UnlockAgvCommand>
    {
        private readonly IAgvRegistry _agvRegistry;
        private readonly IBus _bus;

        /// <summary>
        /// <inheritdoc cref="LockAndUnlockAgvCommandHandler"/>
        /// </summary>
        /// <param name="agvRegistry"></param>
        /// <param name="bus"></param>
        public LockAndUnlockAgvCommandHandler(IAgvRegistry agvRegistry, IBus bus)
        {
            _agvRegistry = agvRegistry;
            _bus = bus;
        }

        /// <summary>
        /// <inheritdoc cref="LockAndUnlockAgvCommandHandler"/>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="AgvNotFoundException"></exception>
        public async Task Consume(ConsumeContext<LockAgvCommand> context)
        {
            var request = context.Message;
            var agv = _agvRegistry.GetRequiredAgv(request.CorrelationId);

            await _bus.Publish(new LockAgv(agv.CorrelationId));
            await context.RespondAsync(new LockAgvResult(agv.CorrelationId));
        }

        /// <summary>
        /// <inheritdoc cref="LockAndUnlockAgvCommandHandler"/>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="AgvNotFoundException"></exception>
        public async Task Consume(ConsumeContext<UnlockAgvCommand> context)
        {
            var request = context.Message;
            var agv = _agvRegistry.GetRequiredAgv(request.CorrelationId);

            await _bus.Publish(new UnlockAgv(agv.CorrelationId));
            await context.RespondAsync(new UnlockAgvResult(agv.CorrelationId));
        }
    }
}