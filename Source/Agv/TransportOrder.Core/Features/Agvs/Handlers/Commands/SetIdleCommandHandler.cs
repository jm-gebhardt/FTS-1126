using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Configurations.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Commands
{
    /// <summary>
    /// Defines a handler for the SetIdle
    /// Gets the agv from agvRegistry and set status of agv to Idle
    /// </summary>
    [UsedImplicitly]
    public class SetIdleCommandHandler : IConsumer<SetIdleCommand>
    {
        private readonly IAgvRegistry _agvRegistry;
        private readonly IBus _bus;

        /// <summary>
        /// <inheritdoc cref="SetIdleCommandHandler"/>
        /// </summary>
        /// <param name="agvRegistry"></param>
        /// <param name="bus"></param>
        public SetIdleCommandHandler(IAgvRegistry agvRegistry, IBus bus)
        {
            _agvRegistry = agvRegistry;
            _bus = bus;
        }

        /// <summary>
        /// <inheritdoc cref="SetIdleCommandHandler"/>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<SetIdleCommand> context)
        {
            var request = context.Message;
            var agv = _agvRegistry.GetAgv(request.CorrelationId);
            if (agv == null)
            {
                throw new NotFoundException(nameof(agv), nameof(request.CorrelationId), request.CorrelationId);
            }

            if (agv.CurrentState != nameof(AgvStateMachine.Discovered) && agv.CurrentState != nameof(AgvStateMachine.Locked))
            {
                throw new NotInCorrectStateException("Agv cannot be released because it is not in valid state to switch");
            }

            await _bus.Publish(new ForceReleaseAgv(agv.CorrelationId));
            await context.RespondAsync(new SetIdleResult());
        }
    }

}