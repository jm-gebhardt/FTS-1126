using Microsoft.Extensions.Configuration;


namespace Gebhardt.StoreWare.Agv.TransportOrder
{
    /// <summary />
    internal class Worker : Common.IWorker
    {
        [JetBrains.Annotations.UsedImplicitly]
        IConfiguration _config;

        /// <summary />
        public Worker(IConfiguration config)
        {
            _config = config;
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
