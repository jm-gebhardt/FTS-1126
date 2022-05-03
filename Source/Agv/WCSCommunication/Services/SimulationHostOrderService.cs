using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Commands;
using Gebhardt.StoreWare.Agv.WCSCommunication.CmdOptions;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Services
{
    /// <summary>
    /// Simulates the creation of host orders
    /// </summary>
    public class SimulationHostOrderService : BackgroundService
    {
        private readonly ILogger<SimulationHostOrderService> _logger;
        private readonly IBus _bus;
        private readonly string[] _simSources;
        private readonly string[] _simDestinations;
        private readonly int _simInterval;

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
            _simInterval = simulationOptions.Interval;
        }

        /// <summary>
        /// Execute background service
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await SimulateHostOrders(cancellationToken);
        }

        /// <summary>
        /// Creates dummy HostOrders for simulation
        /// </summary>
        private async Task SimulateHostOrders(CancellationToken cancellationToken)
        {
            Random rnd = new();
            int id = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                string orderId = $"{id++ % 9999 + 1:D4}"; // Cycle order id [0001..9999]
                string src = _simSources[rnd.Next(_simSources.Length)]; // Get random source and destination
                string dst = _simDestinations[rnd.Next(_simDestinations.Length)];
                string leNo = "9999";

                // Send message
                await _bus.Publish<IHandleHostOrder>(new
                {
                    OrderId = orderId,
                    Source = src,
                    Dest = dst,
                    LeNo = leNo,
                    Creator = HostOrderCreator.Simulation
                }, cancellationToken);

                _logger.LogInformation($"Simulation HostOrder created: OrderId = {orderId}, Source = {src}, Dest = {dst}, LeNo = {leNo}");
                await Task.Delay(_simInterval * 1000, cancellationToken);
            }
        }
    }
}
