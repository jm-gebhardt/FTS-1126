using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;


namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Consumers
{
    /// <summary>
    /// Simulate processing of transport order messages.
    /// </summary>
    public class ExecuteTransportOrderConsumerSim :
        IConsumer<IExecuteTransportOrder>
    {
        private const string IntervalArgumentName = "interval";
        private readonly ILogger<ExecuteTransportOrderConsumerSim> _logger;

        private readonly Random _simInterval;
        private readonly int _minInterval;
        private readonly int _maxInterval;

        /// <summary>
        /// Create consumer with message handler delegate for simulation.
        /// </summary>
        /// <param name="config">Configuration object (mainly used to access command line options for simulation).</param>
        /// <param name="logger">Mandatory logger facility.</param>
        [JetBrains.Annotations.PublicAPI]
        public ExecuteTransportOrderConsumerSim(IConfiguration config, ILogger<ExecuteTransportOrderConsumerSim> logger)
        {
            _logger = logger;

            if (config.GetValue<IEnumerable<int>>(IntervalArgumentName) is List<int> a)
            {
                if (a.Count != 2)
                {
                    throw new ArgumentException("Invalid argument", IntervalArgumentName);
                }
                _minInterval = a[0];
                _maxInterval = a[1];
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
                   context.Publish<ITransportOrderCompleted>(new { context.Message.OrderId }, context.CancellationToken);
                   _logger.Log(LogLevel.Debug, "TransportOrderCompleted: {0}", context.Message.OrderId);
               });
        }
    }
}
