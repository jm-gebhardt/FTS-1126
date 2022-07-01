using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;


namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services
{
    /// <summary>
    /// Handle graph information from AGV.
    /// </summary>
    /// <remarks>
    /// This is passed to the service AgvManagement to have kept DeviceCommunication as a simple proxy.
    /// </remarks>
    [JetBrains.Annotations.UsedImplicitly]
    public class AgvGraphConsumer : IConsumer<IGraph>
    {
        private readonly ILogger<AgvGraphConsumer> _logger;


        /// <summary>
        /// Create consumer with message handler delegate.
        /// </summary>
        /// <param name="logger">Mandatory logger facility.</param>
        public AgvGraphConsumer(ILogger<AgvGraphConsumer> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Consume graph message from AGV and transform this to an internal <see cref="IAgvGraphReceived"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Message content is invalid and cannot be processed.</exception>
        public async Task Consume(ConsumeContext<IGraph> context)
        {
            var parser = new MessageParsers.GraphParser(context.Message);

            try
            {
                await context.Publish<IAgvGraphReceived>(new
                {
                    CorrelationId = parser.SerialNumber.ToGuid(),
                    AgvId = parser.SerialNumber,
                    Nodes = parser.Nodes.ToArray(),
                    Edges = parser.Edges.ToArray()
                }, context.CancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Message IGraph has invalid content: {Graph}.", context.Message);
                throw;
            }
        }
    }
}