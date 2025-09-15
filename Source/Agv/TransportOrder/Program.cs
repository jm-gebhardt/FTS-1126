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

var initialLogger = InitialLogger.Create<Program>();

try
{
    ProductionOptions? productionOptions = null;
    SimulationOptions? simulationOptions = null;
    Parser.Default.ParseArguments<ProductionOptions, SimulationOptions>(args)
        .WithParsed<ProductionOptions>(options => { productionOptions = options; })
        .WithParsed<SimulationOptions>(options => { simulationOptions = options; })
        .WithNotParsed(errors => { initialLogger.LogWarning("Invalid command line argument(s): {Errors}", string.Join(", ", errors)); });

    if (productionOptions == null && simulationOptions == null)
    {
        initialLogger.LogError("Must run in either production or simulation mode");
        return 1;
    }

    if (productionOptions != null && simulationOptions != null)
    {
        initialLogger.LogError("Cannot run in both production and simulation mode");
        return 1;
    }

    var hostBuilder = GebhardtStartup.CreateHostBuilder(args)
        .AddMassTransit(initialLogger)
        .AddTransportOrderCore(productionOptions != null ? productionOptions : simulationOptions!);

    hostBuilder.ConfigureWebHost(host =>
    {
        host.UseKestrel()
            .AddTransportOrderWebApi(simulationOptions != null)
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
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
    return 1;
}