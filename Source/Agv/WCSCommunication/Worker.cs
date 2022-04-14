using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Consumers;

namespace Gebhardt.StoreWare.Agv.WCSCommunication
{
    /// <summary />
    internal class Worker : Common.IMassTransitWorker
    {
        private readonly IBus _bus;

        /// <summary />
        public Worker(IBus bus)
        {
            _bus = bus;

            Common.DummyReplyHandler handler = new();

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
            int id = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                DummyRequest request = new() { Id = id++, Content = "HostOrder Request" };
                await _bus.Publish(request, stoppingToken);
                Console.WriteLine($"Request send: {request}");

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
