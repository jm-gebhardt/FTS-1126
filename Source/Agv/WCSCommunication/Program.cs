using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommandLine;
using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.WCSCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;


var initialLogger = Gebhardt.StoreWare.Agv.Common.Logging.InitialLogger.Create<Program>();
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
                    ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration, x =>
                    {
                        x.AddConsumer<SimulationConfirmHostOrderConsumer>();
                    });
                }).Build();
            host.Run();
        });

    return 0;
}
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception.");
    return 1;
}
