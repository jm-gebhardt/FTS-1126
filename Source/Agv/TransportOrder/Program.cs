using CommandLine;
using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.TransportOrder;
using Gebhardt.Acc.TransportOrder.Core.CmdOptions;
using Gebhardt.Acc.TransportOrder.Core.Configurations;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.Acc.TransportOrder.WebApi.Extensions;
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
        hostBuilder.ConfigureWebHost(host =>
        {
            host.UseKestrel()
                .AddTransportOrderWebApi()
                .ConfigureAppConfiguration((_, config) =>
                {
                    ConfigurationTypeManager.Instance.InitializeConfigurationTypes();
                    config.AddDefaultConfiguration(ConfigurationTypeManager.Instance);
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
        }

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