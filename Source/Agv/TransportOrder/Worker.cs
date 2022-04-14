using Microsoft.Extensions.Configuration;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Consumers;

namespace Gebhardt.StoreWare.Agv.TransportOrder
{
    /// <summary />
    internal class Worker : Common.IMassTransitWorker
    {
        /// <summary />
        [JetBrains.Annotations.UsedImplicitly]
        public Worker(IConfiguration config, IBus bus)
        {
            Common.DummyRequestHandler handler = new(bus);

            bus.ConnectReceiveEndpoint("DummyQueue", x =>
            {
                x.Consumer(() => new DummyConsumer(handler));
            });
        }

        /// <summary />
        public void Start()
        {
            Console.WriteLine("TODO: Implement initialization here.");
        }

        /// <summary />
        public async Task Run(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine("TODO: Implement service execution stuff (main loop) here.");
                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            }
        }

        /// <summary />
        public void Stop()
        {
            Console.WriteLine("TODO: Implement deinitialization here.");
        }
    }
}
