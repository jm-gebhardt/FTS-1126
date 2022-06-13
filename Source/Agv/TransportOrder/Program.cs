using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommandLine;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.States;


var initialLogger = Gebhardt.StoreWare.Agv.Common.Logging.InitialLogger.Create<Program>();
try
{
    Parser.Default.ParseArguments<SimulateOptions>(args)
        .WithParsed(simulationOptions =>
        {
            using var host = GebhardtStartup.CreateHostBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddScoped(_ => simulationOptions);
                    ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration, x =>
                    {
                        x.AddSagaStateMachine<OrderStateMachine, OrderSaga>().InMemoryRepository();
                        x.AddSaga<AgvConsumerSaga>().InMemoryRepository();
                        x.AddConsumer<RoundRobinAssignAgvConsumer>();
                    });
                }).Build();
            host.Run();
        });

    return 0;
}
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
    return 1;
}
