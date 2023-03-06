using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Commands
{
    /// <summary>
    /// Defines a handler for the EmitRequestGraph
    /// Gets the agv from agvRegistry and set status of agv from Discovered to Idle
    /// </summary>
    [UsedImplicitly]
    public class RequestAgvGraphCommandHandler : IConsumer<RequestAgvGraphCommand>
    {
        private readonly IAgvRegistry _agvRegistry;
        private readonly IBus _bus;

        /// <summary>
        /// <inheritdoc cref="RequestAgvGraphCommandHandler"/>
        /// </summary>
        /// <param name="agvRegistry"></param>
        /// <param name="bus"></param>
        public RequestAgvGraphCommandHandler(IAgvRegistry agvRegistry, IBus bus)
        {
            _agvRegistry = agvRegistry;
            _bus = bus;
        }

        /// <summary>
        /// <inheritdoc cref="RequestAgvGraphCommandHandler"/>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<RequestAgvGraphCommand> context)
        {
            var request = context.Message;
            var agv = _agvRegistry.GetAgv(request.CorrelationId);

            if (agv == null)
            {
                throw new NotFoundException(nameof(agv), nameof(request.CorrelationId), request.CorrelationId);
            }

            await _bus.Publish(new RequestAgvGraph(agv.CorrelationId, agv.AgvId));
            await context.RespondAsync(new RequestAgvGraphResult());
        }
    }

}