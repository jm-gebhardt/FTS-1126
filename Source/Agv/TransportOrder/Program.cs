using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommandLine;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using Gebhardt.StoreWare.Agv.Common.Messages;

var initialLogger = Gebhardt.StoreWare.Agv.Common.Logging.InitialLogger.Create<Program>();
try
{
    IHost? host = null;

    Parser.Default.ParseArguments<ProductionOptions, SimulateOptions>(args)
        // Simulation Mode
        .WithParsed(simulationOptions =>
        {
            host = GebhardtStartup.CreateHostBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddScoped(_ => simulationOptions);

                    services.AddSingleton<RoundRobinAgvRegistry>();
                    ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration, x =>
                    {
                        x.AddSagaStateMachine<OrderStateMachine, OrderSaga>().InMemoryRepository();
                        x.AddSaga<AgvConsumerSaga>().InMemoryRepository();
                        x.AddConsumer<RoundRobinAssignAgvConsumer>();
                    });
                })
            .Build();
        })
        // Production Mode
        .WithParsed<ProductionOptions>(_ =>
        {
            host = GebhardtStartup.CreateHostBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<ConcurrentQueue<KeyValuePair<Guid, Order>>>();
                    services.AddSingleton<IAgvRegistry>((_) => new InMemoryAgvRegistry());
                    ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration, x =>
                    {
                        x.AddSagaStateMachine<OrderStateMachine, OrderSaga>().InMemoryRepository();
                        x.AddSaga<AgvConsumerSaga>().InMemoryRepository();
                        x.AddConsumer<AssignAgvConsumer>();
                        x.AddConsumer<AgvStateChangedConsumer>();
                    });
                })
            .Build();
        });

    // By using the generic overload of WithParsed above host will be null for invalid command line arguments.
    if (host != null)
    {
        host.Run();
        return 0;
    }
    else
    {
        initialLogger.LogWarning("Invalid command line argument(s)");
        return 1;
    }
}
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
    return 1;
}
