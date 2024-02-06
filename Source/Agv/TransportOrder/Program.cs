using CommandLine;
using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.Common.Configurations;
using Gebhardt.StoreWare.Agv.Common.Logging;
using Gebhardt.StoreWare.Agv.TransportOrder;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

const string quartzSchedulerName = "MassTransit-TransportOrder";
const string quartzSchedulerId = "AUTO";
const string quartzQueueName = "quartz-TransportOrder";

var initialLogger = InitialLogger.Create<Program>();

try
{
    IHostBuilder? hostBuilder = null;

    Parser.Default.ParseArguments<ProductionOptions>(args)
        .WithParsed(productionOptions =>
        {
            hostBuilder = GebhardtStartup.CreateHostBuilder(args)
                .AddMassTransit(initialLogger, quartzSchedulerName, quartzSchedulerId, quartzQueueName)
                .AddTransportOrderCore(productionOptions);
        });

    // By using the generic overload of WithParsed above hostBuilder will be null for invalid command line arguments.
    if (hostBuilder != null)
    {
        DatabaseConfigurationSource<TransportOrderDbContext>? databaseConfigurationSource = null;
        hostBuilder.ConfigureWebHost(host =>
        {
            host.UseKestrel()
                .AddTransportOrderWebApi()
                .ConfigureAppConfiguration((_, config) =>
                {
                    ConfigurationTypeManager.Instance.InitializeConfigurationTypes();
                    config.AddDefaultConfiguration(ConfigurationTypeManager.Instance);

                    databaseConfigurationSource = config.AddDatabaseConfiguration<TransportOrderDbContext>(ConfigurationTypeManager.Instance, initialLogger, 0, true);
                })
                .Configure(app =>
                {
                    app.UseMetricServer();
                    app.UseTransportOrderWebApi();
                    app.UseDeveloperExceptionPage();
                });
        });

        var host = hostBuilder.Build();

        using (var scope = host.Services.CreateScope())
        {
            var transportOrderDb = scope.ServiceProvider.GetRequiredService<TransportOrderDbContext>();
            await transportOrderDb.Database.MigrateAsync();

            var dbConfigurationSeeder = scope.ServiceProvider.GetService<DatabaseConfigurationSeeder<TransportOrderDbContext>>()!;
            await dbConfigurationSeeder.SeedDefaultConfigurationAsync();
        }

        // we need to set the DbContext manually and after instantiation, because the AppConfiguration is built before the services (dbContext) are built
        databaseConfigurationSource!.UpdateDbContextFactory(host.Services.GetRequiredService<IDbContextFactory<TransportOrderDbContext>>());

        host.Run();
        return 0;
    }
    initialLogger.LogWarning("Invalid command line argument(s)");
    return 1;
}
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
    return 1;
}