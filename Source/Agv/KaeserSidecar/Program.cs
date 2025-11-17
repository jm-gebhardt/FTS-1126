using CommandLine;
using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.KaeserSidecar.CmdOptions;
using Gebhardt.Acc.KaeserSidecar.Configuration;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var initialLogger = InitialLogger.Create<Program>();

try
{
    SimulateOptions? simulateOptions = null;
    Parser.Default.ParseArguments<ProductionOptions, SimulateOptions>(args)
        .WithParsed<SimulateOptions>(o =>
        {
            simulateOptions = o;
        });

    var hostBuilder = KaeserSidecarHostBuilder.CreateHostBuilder(args, simulateOptions, initialLogger);

    var host = hostBuilder.Build();
    using (var scope = host.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<KaeserSidecarDbContext>();
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