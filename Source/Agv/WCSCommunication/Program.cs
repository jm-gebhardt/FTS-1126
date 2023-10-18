using CommandLine;
using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.Common.Configurations.Extensions;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Logging;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.WCSCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.WCSCommunication.Configuration;
using Gebhardt.StoreWare.Agv.WCSCommunication.Domain.Repositories;
using Gebhardt.StoreWare.Agv.WCSCommunication.Domain.Services;
using Gebhardt.StoreWare.Agv.WCSCommunication.Messages.Commands;
using Gebhardt.StoreWare.Agv.WCSCommunication.Persistence.DataAccess;
using Gebhardt.StoreWare.Agv.WCSCommunication.Persistence.Repositories;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services.CommandHandler;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services.EventHandler;
using Gebhardt.StoreWare.Agv.WCSCommunication.WcsApi.DbInterface;
using Gebhardt.StoreWare.Agv.WCSCommunication.WcsApi.SimulationMode;
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
                    services.AddTransient<IHostCommunicator, SimulationHostCommunicator>();
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
                    services.AddTransient<IHostCommunicator, WcsDbCommunicator>();
                });
        });

    hostBuilder?.ConfigureServices((context, services) =>
    {
        services.AddDbContext<WcsCommunicationDbContext>(builder =>
        {
            string? connectionString = context.Configuration.GetConnectionString(WcsCommunicationDbContext.ConfigSectionName);
            builder.UseSqlServer(connectionString, m => { m.MigrationsHistoryTable(WcsCommunicationDbContext.MigrationTableName); });
        });
        services.AddTransient<IHostOrderRepository, HostOrderRepository>();
        services.ConfigureServicesMassTransit(context.Configuration, x =>
        {
            x.AddConsumer<ActionGroupEventHandler>();
            x.AddConsumer<HostOrderCommandHandler>(c =>
            {
                var partitioner = c.CreatePartitioner(16);
                partitioner.PartitionMessageById<CreateHostOrder, HostOrderCommandHandler>(c);
                partitioner.PartitionMessageById<RemoveInvalidHostOrder, HostOrderCommandHandler>(c);
                partitioner.PartitionMessageById<FinishHostOrder, HostOrderCommandHandler>(c);
                partitioner.PartitionMessageById<FinishHostOrderCancellation, HostOrderCommandHandler>(c);
                partitioner.PartitionMessageById<HandleCancellationFailure, HostOrderCommandHandler>(c);
                partitioner.PartitionMessageById<HandleHostOrderStart, HostOrderCommandHandler>(c);
                partitioner.PartitionMessageById<HandleHostOrderFailure, HostOrderCommandHandler>(c);
            });
        }, (b, c) => c.UseConsumeFilter(typeof(ActionEventConsumeFilter<>), b));
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
            var dbAmc = scope.ServiceProvider.GetService<WcsCommunicationDbContext>();
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