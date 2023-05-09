using CommandLine;
using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.Common.Configurations;
using Gebhardt.StoreWare.Agv.TransportOrder.Core;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Saga;

var initialLogger = Gebhardt.StoreWare.Agv.Common.Logging.InitialLogger.Create<Program>();

try
{
    IHostBuilder? hostBuilder = null;

    Parser.Default.ParseArguments<ProductionOptions>(args)
        .WithParsed(productionOptions =>
        {
            hostBuilder = GebhardtStartup.CreateHostBuilder(args)
                .AddTransportOrderCore(initialLogger, productionOptions);
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
                });
        });

        var host = hostBuilder.Build();

        using (var scope = host.Services.CreateScope())
        {
            // Apply migrations if SagaPersistenceDbContext is available
            var db1 = scope.ServiceProvider.GetRequiredService<SagaPersistenceDbContext>();
            await db1.Database.MigrateAsync();

            var db2 = scope.ServiceProvider.GetRequiredService<TransportOrderDbContext>();
            await db2.Database.MigrateAsync();

            var dbConfigurationSeeder = scope.ServiceProvider.GetService<DatabaseConfigurationSeeder<TransportOrderDbContext>>()!;
            await dbConfigurationSeeder.SeedDefaultConfigurationAsync();
        }

        // we need to set the DbContext manually and after instantiation, because the AppConfiguration is built before the services (dbContext) are built
        databaseConfigurationSource!.UpdateDbContextFactory(host.Services.GetRequiredService<IDbContextFactory<TransportOrderDbContext>>());

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
