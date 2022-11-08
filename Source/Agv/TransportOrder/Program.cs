using CommandLine;
using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

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
        hostBuilder.ConfigureWebHost(host =>
        {
            host.UseKestrel()
                .AddTransportOrderWebApi()
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
