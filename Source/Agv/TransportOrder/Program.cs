using CommandLine;
using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.TransportOrder;
using Gebhardt.Acc.TransportOrder.Core.CmdOptions;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var initialLogger = InitialLogger.Create<Program>();

try
{
    var swaggerCli = string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "SwaggerCli");

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

    var hostBuilder = TransportOrderHostBuilder.CreateHostBuilder(args, productionOptions != null ? productionOptions! : simulationOptions!, initialLogger, simulationOptions != null);

    var host = hostBuilder.Build();
    if (swaggerCli)
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransportOrderDbContext>();
        dbContext.SetCommandTimeOut();
        await dbContext.Database.MigrateAsync();
    }

    host.Run();
    return 0;
}
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
    return 1;
}