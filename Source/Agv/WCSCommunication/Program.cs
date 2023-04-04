using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommandLine;
using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.WCSCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.WCSCommunication.Db;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;
using Gebhardt.StoreWare.Agv.WCSCommunication.States;
using Gebhardt.StoreWare.Agv.WCSCommunication.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Gebhardt.StoreWare.Agv.Common.Configurations.Extensions;

var initialLogger = Gebhardt.StoreWare.Agv.Common.Logging.InitialLogger.Create<Program>();
try
{
    IHost? host = null;
    bool simulation = false;
    
    Parser.Default.ParseArguments<ProductionOptions, SimulateOptions>(args)
        // Simulation Mode
        .WithParsed<SimulateOptions>(simulationOptions =>
        {
            simulation = true;
            host = GebhardtStartup.CreateHostBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddDbContextFactory<WcsSagaDbContext>(builder =>
                    {
                        string? connectionString = context.Configuration.GetConnectionString(WcsSagaDbContext.ConfigSectionName);
                        builder.UseSqlServer(connectionString, m =>
                        {
                            m.MigrationsHistoryTable(WcsSagaDbContext.MigrationTableName);
                        });
                    });
                    services.AddSingleton(_ => simulationOptions);
                    services.AddHostedService<PollingBackgroundService<SimulationHostOrderService>>();
                    services.AddSingleton<SimulationHostOrderService>();
                    services.ConfigureServicesMassTransit(context.Configuration, x =>
                    {
                        x.AddConsumer<SimulationConfirmHostOrderConsumer>();
                        x.AddSagaStateMachine<HostOrderStateMachine, HostOrderSaga>()
                            .AddPersistence<HostOrderSaga, WcsSagaDbContext>();
                    });
                }).Build();
        })
        // Production Mode
        .WithParsed<ProductionOptions>( _ =>
        {
            host = GebhardtStartup.CreateHostBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddDbContextFactory<WcsHostDbContext>(builder =>
                    {
                        string? connectionString = context.Configuration.GetConnectionString(WcsHostDbContext.ConfigSectionName);
                        builder.UseSqlServer(connectionString);
                    });
                    services.AddDbContextFactory<WcsSagaDbContext>(builder =>
                    {
                        string? connectionString = context.Configuration.GetConnectionString(WcsSagaDbContext.ConfigSectionName);
                        builder.UseSqlServer(connectionString, m =>
                        {
                            m.MigrationsHistoryTable(WcsSagaDbContext.MigrationTableName);
                        });
                    });
                    services.AddHostedService<PollingBackgroundService<WcsOrderHandler>>();
                    services.AddSingleton<WcsOrderHandler>();
                    services.ConfigureServicesMassTransit(context.Configuration, x =>
                    {
                        x.AddConsumer<WcsHostOrderConsumer>();
                        x.AddSagaStateMachine<HostOrderStateMachine, HostOrderSaga>()
                            .AddPersistence<HostOrderSaga, WcsSagaDbContext>();
                    });
                })
                .Build();
        });

    // By using the generic overload of WithParsed above host will be null for invalid command line arguments.
    if (host != null)
    {
        using (var scope = host.Services.CreateScope())
        {
            if (!simulation)
            {
                var dbWcs = scope.ServiceProvider.GetService<WcsHostDbContext>();
                dbWcs?.Database.Migrate();
            }
            var dbAmc = scope.ServiceProvider.GetService<WcsSagaDbContext>();
            dbAmc?.Database.Migrate();
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
