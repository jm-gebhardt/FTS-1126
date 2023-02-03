using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages;

namespace Gebhardt.StoreWare.Agv.Common.Services
{
    /// <summary>
    /// Handle MassTransit bus events and provide expose bus state information
    /// </summary><remarks>
    /// Can be used to inject to services when they need to know about bus state.
    /// </remarks>
    [JetBrains.Annotations.PublicAPI]
    public class MassTransitBusEventConsumer : IConsumer<MassTransitBusEvent>
    {
        private readonly ILogger<MassTransitBusEventConsumer> _logger;


        /// <summary>
        /// Returns true when MassTransit bus has been started successfully
        /// </summary>
        [JetBrains.Annotations.PublicAPI]
        public bool BusStarted { get; private set; }

        /// <summary>
        /// Create consumer with message handler delegate.
        /// </summary>
        /// <param name="logger">Mandatory logger facility.</param>
        public MassTransitBusEventConsumer(ILogger<MassTransitBusEventConsumer> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Handle MassTransit bus events
        /// </summary>
        /// <param name="context"></param>
        public Task Consume(ConsumeContext<MassTransitBusEvent> context)
        {
            if (context.Message.Type == MassTransitBusEvent.EventType.PostStarted)
            {
                BusStarted  = true;
                _logger.LogInformation("{MessageType} of type {EventType} occured", nameof(MassTransitBusEvent), context.Message.Type);
            }
            return Task.CompletedTask;
        }
    }
}
