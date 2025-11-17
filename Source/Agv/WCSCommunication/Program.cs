using CommandLine;
using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.WCSCommunication;
using Gebhardt.Acc.WCSCommunication.CmdOptions;
using Gebhardt.Acc.WCSCommunication.Persistence.DataAccess;
using Gebhardt.Acc.WCSCommunication.WcsApi.DbInterface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var initialLogger = InitialLogger.Create<Program>();
try
{
    bool isSimulation = false;

    SimulateOptions? simulateOptions = null;

    Parser.Default.ParseArguments<ProductionOptions, SimulateOptions>(args)
        // Simulation Mode
        .WithParsed<SimulateOptions>(simulationOptions =>
        {
            isSimulation = true;
            simulateOptions = simulationOptions;
        });

    var hostBuilder = WcsCommunicationHostBuilder.CreateHostBuilder(args, isSimulation, simulateOptions);

    var host = hostBuilder.Build();

    using (var scope = host.Services.CreateScope())
    {
        if (!isSimulation)
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
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
    return 1;
}