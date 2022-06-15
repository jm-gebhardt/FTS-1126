using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Services
{
	/// <summary>
	/// Simulate finishing of host orders
	/// </summary>
	public class SimulationConfirmHostOrderConsumer :
        IConsumer<IHostOrderHandlingSucceeded>
    {
        private readonly ILogger<SimulationConfirmHostOrderConsumer> _logger;

        /// <summary>
        /// Create consumer with message handler delegate for simulation.
        /// </summary>
        /// <param name="logger">Mandatory logger facility.</param>
        [JetBrains.Annotations.PublicAPI]
        public SimulationConfirmHostOrderConsumer(ILogger<SimulationConfirmHostOrderConsumer> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Simulation: Handle message <see cref="IHostOrderHandlingSucceeded"/>.
        /// <para>Logs received message and then discards it.</para>
        /// </summary>
        /// <param name="context">Context of the consumer wrapping details surrounding the inbound message.</param>
        public async Task Consume(ConsumeContext<IHostOrderHandlingSucceeded> context)
        {
            await Task.Run(() => // Await Task to satisfy async
            {
                _logger.LogInformation("HostOrder handling succeeded for OrderId {OrderId} / {CorrelationId}", 
                    context.Message.OrderId, context.Message.CorrelationId
                    );
            });
        }
    }
}
