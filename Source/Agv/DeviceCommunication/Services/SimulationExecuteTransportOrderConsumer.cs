using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
using Microsoft.Extensions.Logging;
using MassTransit;
using JetBrains.Annotations;
using Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services
{
    /// <summary>
    /// Simulate processing of transport order messages.
    /// </summary>
    [UsedImplicitly]
    public class SimulationExecuteTransportOrderConsumer :
        IConsumer<IExecuteTransportOrder>
    {
	    private readonly ILogger<SimulationExecuteTransportOrderConsumer> _logger;

        private readonly Random _simInterval;
        private readonly int _minInterval;
        private readonly int _maxInterval;


        /// <summary>
        /// Create dummy consumer with message handler delegate
        /// </summary>
        public SimulationExecuteTransportOrderConsumer(ILogger<SimulationExecuteTransportOrderConsumer> logger, SimulateOptions simulationOptions)
        {
	        _logger = logger;
	        if (simulationOptions.Interval != null)
	        {
		        int[] interval = simulationOptions.Interval.ToArray();
		        if (interval.Length == 2)
		        {
			        _minInterval = interval[0];
			        _maxInterval = interval[1];
		        }
			}
	        _simInterval = new();
        }

        /// <summary>
        /// Simulation: Handle message <see cref="IExecuteTransportOrder"/>.
        /// <para>Responds with <see cref="ITransportOrderCompleted"/> after a random delay.</para>
        /// </summary>
        /// <param name="context">Context of the consumer wrapping details surrounding the inbound message.</param>
        public async Task Consume(ConsumeContext<IExecuteTransportOrder> context)
        {
            await Task.Delay(1000 * _simInterval.Next(_minInterval, _maxInterval + 1), context.CancellationToken)
               .ContinueWith(_ =>
               {
	               context.Publish<ITransportOrderCompleted>(new { context.Message.CorrelationId, context.Message.Order.OrderId }, context.CancellationToken);
                   _logger.LogInformation("TransportOrderCompleted: {OrderId}", context.Message.Order.OrderId);
               });
        }
    }
}
