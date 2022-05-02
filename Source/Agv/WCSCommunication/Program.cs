using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommandLine;
using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.WCSCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;

try
{
    Parser.Default.ParseArguments<SimulateOptions>(args)
        .WithParsed(simulationOptions =>
        {
            using var host = GebhardtStartup.CreateHostBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton(_ => simulationOptions);
                    services.AddHostedService<SimulationHostOrderService>();
                    ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration);
                }).Build();
            host.Run();
        });
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

return 0;