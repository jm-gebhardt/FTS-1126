using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.KaeserConveyorTelegramApiServer.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var initialLogger = InitialLogger.Create<Program>();
try
{
    var hostBuilder = KaeserConveyorTelegramApiServerHostBuilder.CreateHostBuilder(args);
    var host = hostBuilder.Build();
    host.Run();
}
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
}