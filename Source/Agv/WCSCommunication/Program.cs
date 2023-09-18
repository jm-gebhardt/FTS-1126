using CommandLine;
using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.Common.Configurations.Extensions;
using Gebhardt.StoreWare.Agv.Common.Logging;
using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.WCSCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.WCSCommunication.Db;
using Gebhardt.StoreWare.Agv.WCSCommunication.Persistence;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;
using Gebhardt.StoreWare.Agv.WCSCommunication.States;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var initialLogger = InitialLogger.Create<Program>();
try
{
    IHostBuilder? hostBuilder = null;
    bool simulation = false;

    Parser.Default.ParseArguments<ProductionOptions, SimulateOptions>(args)
        // Simulation Mode
        .WithParsed<SimulateOptions>(simulationOptions =>
        {
            simulation = true;
            hostBuilder = GebhardtStartup.CreateHostBuilder(args)
                .ConfigureServices((_, services) =>
                {
                    services.AddHostedService<PollingBackgroundService<SimulationHostOrderService>>();

                    services.AddSingleton(_ => simulationOptions);
                    services.AddSingleton<SimulationHostOrderService>();
                });
        })
        // Production Mode
        .WithParsed<ProductionOptions>(_ =>
        {
            hostBuilder = GebhardtStartup.CreateHostBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddDbContextFactory<WcsHostDbContext>(builder =>
                    {
                        string? connectionString = context.Configuration.GetConnectionString(WcsHostDbContext.ConfigSectionName);
                        builder.UseSqlServer(connectionString);
                    });

                    services.AddHostedService<PollingBackgroundService<WcsOrderHandler>>();

                    services.AddSingleton<WcsOrderHandler>();
                });
        });

    hostBuilder?.ConfigureServices((context, services) =>
    {
        services.AddDbContextFactory<WcsSagaDbContext>(builder =>
        {
            string? connectionString = context.Configuration.GetConnectionString(WcsSagaDbContext.ConfigSectionName);
            builder.UseSqlServer(connectionString, m =>
            {
                m.MigrationsHistoryTable(WcsSagaDbContext.MigrationTableName);
            });
        });
        services.AddSingleton<IStateObserver<HostOrderSaga>, HostOrderStateObserver>();
        services.ConfigureServicesMassTransit(context.Configuration, x =>
        {
            x.AddSagaStateMachine<HostOrderStateMachine, HostOrderSaga>(cfg => cfg.UseMessageRetry(c =>c.Interval(5, TimeSpan.FromSeconds(1))))
                .AddPersistence<HostOrderSaga, WcsSagaDbContext>();
        });
    });

    var host = hostBuilder?.Build();

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
