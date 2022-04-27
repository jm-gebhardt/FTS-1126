using Microsoft.Extensions.Hosting;
using Gebhardt.StoreWare.Agv.Common;

try
{
    using var host = GebhardtStartup.CreateHostBuilder(args)
        .ConfigureServices((context, services) =>
        {
            // Configure MassTransit with RabbitMQ if the worker requires it
            ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration);
        }).Build();
    host.Run();
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

return 0;