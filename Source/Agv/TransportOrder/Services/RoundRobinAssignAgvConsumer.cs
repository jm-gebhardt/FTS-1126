using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{
    /// <summary>
    /// Round Robin AgvId Dispatcher responding to AssignAgv
    /// This is a very simple implementation which is not aware of the AGV current state
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class RoundRobinAssignAgvConsumer : IConsumer<AssignAgv>
    {
        private readonly RoundRobinAgvRegistry _agvRegistry;

        /// <summary>
        /// Round Robin AGV Dispatcher
        /// </summary>
        public RoundRobinAssignAgvConsumer(IAgvRegistry registry)
        {
            _agvRegistry = (RoundRobinAgvRegistry)registry;
        }

        /// <summary>
        /// Respond to AGV Assignment in a RoundRobin fashion
        /// </summary>
        /// <param name="context"></param>
        public async Task Consume(ConsumeContext<AssignAgv> context)
        {
            string agvId = ChooseAgv();
            var correlationId = agvId.ToGuid();
            await context.Publish(new AgvAssignmentSucceeded(context.Message.CorrelationId, agvId, correlationId));
        }

        private string ChooseAgv()
        {
            return _agvRegistry.Next();
        }
    }
}
