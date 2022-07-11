using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommandLine;
using System.Text.Json.Serialization;
using MassTransit.Serialization;
using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
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
            // Enable serialization/deserialization of enum values as string.
            SystemTextJsonMessageSerializer.Options.Converters.Add(new JsonStringEnumConverter());

            host = GebhardtStartup.CreateHostBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddScoped(_ => options);
                    services.AddSingleton<RoundRobinAgvRegistry>();
                    
                    services.AddDbContextFactory<SagaPersistenceDbContext>(builder =>
                    {
                        builder.UseSqlServer(context.Configuration.GetConnectionString(SagaPersistenceDbContext.ConfigSectionName), m =>
                        {
                            m.MigrationsHistoryTable(SagaPersistenceDbContext.MigrationTableName);
                        });
                    });
                    
                    ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration, x =>
                    {
                        x.AddSagaStateMachine<OrderStateMachine, OrderSaga>()
                            .AddPersistence<OrderSaga, SagaPersistenceDbContext>();
                        x.AddConsumer<RoundRobinAssignAgvConsumer>();
                    });
                }).Build();

        })
        .WithParsed<ProductionOptions>(_ =>
        {
            // Enable serialization/deserialization of enum values as string.
            SystemTextJsonMessageSerializer.Options.Converters.Add(new JsonStringEnumConverter());

            host = GebhardtStartup.CreateHostBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<ConcurrentQueue<KeyValuePair<Guid, Order>>>();
                    services.AddTransient<IAgvRegistry, EfAgvRegistry>();
                    
                    services.AddDbContextFactory<SagaPersistenceDbContext>(builder => 
                    { 
                        builder.UseSqlServer(context.Configuration.GetConnectionString(SagaPersistenceDbContext.ConfigSectionName), m =>
                        {
                            m.MigrationsHistoryTable(SagaPersistenceDbContext.MigrationTableName);
                        }); 
                    });
              
                    try
                    {
                        ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration, x =>
                        {
                            x.AddSagaStateMachine<OrderStateMachine, OrderSaga>()
                                .AddPersistence<OrderSaga, SagaPersistenceDbContext>();
                            x.AddSagaStateMachine<AgvStateMachine, AgvSaga>()
                                .AddPersistence<AgvSaga, SagaPersistenceDbContext>();
                            x.AddConsumer<AssignAgvConsumer>();
                            x.AddRequestClient<ReserveAgv>();
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
