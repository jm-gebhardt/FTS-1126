using Microsoft.Extensions.Configuration;
using MassTransit;


namespace Gebhardt.StoreWare.Agv.DeviceCommunication
{
    /// <summary />
    internal class Worker : Common.IMassTransitWorker
    {
        /// <summary />
        [JetBrains.Annotations.UsedImplicitly]
        public Worker(IConfiguration config, IBus bus)
        {

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
