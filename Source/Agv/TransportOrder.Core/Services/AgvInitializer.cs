using Gebhardt.StoreWare.Agv.Common.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services
{
    /// <summary>
    /// Initialize all AGVs known to the registry.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class AgvInitializer : IConsumer<MassTransitBusEvent>
    {
        private readonly ILogger<AgvInitializer> _logger;
        private readonly IAgvRegistry _agvRegistry;
        private readonly IBus _bus;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="agvRegistry"></param>
        /// <param name="bus"></param>
        public AgvInitializer(ILogger<AgvInitializer> logger, IAgvRegistry agvRegistry, IBus bus)
        {
            _logger = logger;
            _agvRegistry = agvRegistry;
            _bus = bus;
        }

        /// <summary>
        /// Handle MassTransit bus events
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Task completed</returns>
        public Task Consume(ConsumeContext<MassTransitBusEvent> context)
        {
            if (context.Message.Type == MassTransitBusEvent.EventType.PostStarted)
            {
                Initialize();
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Send initialization event in order to initialize all AGVs
        /// </summary>
        private void Initialize()
        {
            foreach (var agv in _agvRegistry.GetAllAgvs())
            {
                _logger.LogInformation("Initializing AGV {AgvId} / {CorrelationId}", agv.AgvId, agv.CorrelationId);
                _bus.Publish(new AgvStateMachine.AgvInitializeMessage(agv.CorrelationId, agv.AgvId));
            }
        }
    }
}
