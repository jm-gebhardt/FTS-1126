using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.WCSCommunication.CmdOptions;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Services
{
    /// <summary>
    /// Simulates the creation of host orders
    /// </summary>
    public class SimulationHostOrderService : IPollable
    {
        /// <summary>
        /// IPollable interval
        /// </summary>
        public int PollingInterval  { get; }
        
        private readonly ILogger<SimulationHostOrderService> _logger;
        private readonly IBus _bus;
        private readonly string[] _simSources;
        private readonly string[] _simDestinations;
        private readonly Random _random = new();
        private int _orderIdCounter;
        
        /// <summary>
        /// Create simulator for creating host order
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="bus">MassTransit message bus</param>
        /// <param name="simulationOptions">Commandline options</param>
        public SimulationHostOrderService(ILogger<SimulationHostOrderService> logger, IBus bus, SimulateOptions simulationOptions)
        {
            _bus = bus;
            _logger = logger;

            _simSources = simulationOptions.Sources != null ? simulationOptions.Sources.ToArray() : Array.Empty<string>();
            _simDestinations = simulationOptions.Destinations != null ? simulationOptions.Destinations.ToArray() : Array.Empty<string>();
            PollingInterval = simulationOptions.Interval * 1000;  // simulation options is specified in seconds
        }

        /// <summary>
        /// Creates dummy HostOrders for simulation
        /// </summary>
        private async Task SimulateHostOrders(CancellationToken cancellationToken)
        {
            
            string orderId = $"{_orderIdCounter++ % 9999 + 1:D4}"; // Cycle order id [0001..9999]
            string src = _simSources[_random.Next(_simSources.Length)]; // Get random source and destination
            string dst = _simDestinations[_random.Next(_simDestinations.Length)];
            string leNo = "9999";

            // Send message
            await _bus.Publish<IHandleHostOrder>(new
            {
                Order = new Order(
                orderId,
                src,
                dst,
                leNo,
                HostOrderCreator.Simulation)
            }, cancellationToken);

            _logger.LogInformation("Simulation HostOrder created: OrderId = {OrderId}, Source = {Src}, Dest = {Dst}, LeNo = {LeNo}", orderId, src, dst, leNo);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        public async Task Poll(CancellationToken cancellationToken = default)
        {
            await SimulateHostOrders(cancellationToken);
        }
    }
}
