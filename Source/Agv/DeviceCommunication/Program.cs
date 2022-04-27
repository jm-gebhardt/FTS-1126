using Microsoft.Extensions.Hosting;
using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Consumers;

try
{
    using var host = GebhardtStartup.CreateHostBuilder(args)
        .ConfigureServices((context, services) =>
        {
            //services.AddOptions<>()

            // Configure MassTransit with RabbitMQ if the worker requires it
            ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration, x =>
            {
                x.AddConsumer<ExecuteTransportOrderConsumerSim>();
            });
        }).Build();
    host.Run();
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

return 0;