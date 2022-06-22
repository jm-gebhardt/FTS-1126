using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommandLine;

using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var initialLogger = Gebhardt.StoreWare.Agv.Common.Logging.InitialLogger.Create<Program>();
try
{
    IHost? host = null;
    
    Parser.Default.ParseArguments<ProductionOptions, SimulateOptions>(args)
        // Simulation Mode
        .WithParsed<SimulateOptions>(options =>
        {
            host = GebhardtStartup.CreateHostBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddScoped(_ => options);
                    services.AddSingleton<RoundRobinAgvRegistry>();
                    if (!options.EnableInMemoryPersistence)
                    {
                        services.AddDbContextFactory<SagaPersistenceDbContext>(builder =>
                        {
                            builder.UseSqlServer(context.Configuration.GetConnectionString(SagaPersistenceDbContext.ConfigSectionName), m =>
                            {
                                m.MigrationsHistoryTable(SagaPersistenceDbContext.MigrationTableName);
                            });
                        });
                    }
                    ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration, x =>
                    {
                        x.AddSagaStateMachine<OrderStateMachine, OrderSaga>()
                            .AddPersistence<OrderSaga, SagaPersistenceDbContext>(options.EnableInMemoryPersistence);

                        x.AddSaga<AgvConsumerSaga>()
                            .AddPersistence<AgvConsumerSaga, SagaPersistenceDbContext>(options.EnableInMemoryPersistence);
                        x.AddConsumer<RoundRobinAssignAgvConsumer>();
                    });
                }).Build();

        })
        .WithParsed<ProductionOptions>(options =>
        {
            host = GebhardtStartup.CreateHostBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<ConcurrentQueue<KeyValuePair<Guid, Order>>>();
                    services.AddSingleton<IAgvRegistry>((_) => new InMemoryAgvRegistry());
                    
                    if (!options.EnableInMemoryPersistence)
                    {
                        services.AddDbContextFactory<SagaPersistenceDbContext>(builder => 
                        { 
                            builder.UseSqlServer(context.Configuration.GetConnectionString(SagaPersistenceDbContext.ConfigSectionName), m =>
                            {
                                m.MigrationsHistoryTable(SagaPersistenceDbContext.MigrationTableName);
                            }); 
                        });
                    }
                    try
                    {
                        ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration, x =>
                        {
                            x.AddSagaStateMachine<OrderStateMachine, OrderSaga>()
                                .AddPersistence<OrderSaga, SagaPersistenceDbContext>(options.EnableInMemoryPersistence);
                            x.AddSaga<AgvConsumerSaga>()
                                .AddPersistence<AgvConsumerSaga, SagaPersistenceDbContext>(options.EnableInMemoryPersistence);
                            x.AddConsumer<AssignAgvConsumer>();
                            x.AddConsumer<AgvStateChangedConsumer>();
                        });
                    }
                    catch (InvalidOperationException e)
                    {
                        initialLogger.LogError(e, "MassTransit could not be started");
                    }
                }).Build();
        });
    // By using the generic overload of WithParsed above host will be null for invalid command line arguments.
    if (host != null)
    {
        using (var scope = host.Services.CreateScope())
        {
            // Apply migrations if SagaPersistenceDbContext is available
            var db = scope.ServiceProvider.GetService<SagaPersistenceDbContext>();
            db?.Database.Migrate();
        }
        
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
