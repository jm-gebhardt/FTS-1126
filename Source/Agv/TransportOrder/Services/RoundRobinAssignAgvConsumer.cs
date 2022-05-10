using Gebhardt.StoreWare.Agv.Common.Messages.Agv.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Agv.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.CmdOptions;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{

    /// <summary>
    /// Round Robin Agv Dispatcher responding to IAssignAGV
    /// This is a very simple implementation which is not aware of the AGV current state
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class RoundRobinAssignAgvConsumer : IConsumer<IAssignAgv>
    {
        private static int _current;
        private const string AgvNamePrefix = "AGV";
        private readonly int _agvCount;
        
        /// <summary>
        /// Round Robin AGV Dispatcher
        /// </summary>
        public RoundRobinAssignAgvConsumer(SimulateOptions options)
        {
            _agvCount = options.Agvs;
        }

        /// <summary>
        /// Respond to AGV Assignment in a RoundRobin fashion
        /// </summary>
        /// <param name="context"></param>
        public async Task Consume(ConsumeContext<IAssignAgv> context)
        {
            await context.Publish<IAgvAssignSucceeded>(new
            {
                context.Message.CorrelationId,
                context.Message.Order.OrderId,
                AssignedAgv = ChooseAgv()
            });
        }

        private string ChooseAgv()
        {
            return $"{AgvNamePrefix}_{Next()}";
        }

        private int Next()
        {
            int next = _current;
            _current = (_current + 1) % _agvCount;
            return next;
        }
    }
}