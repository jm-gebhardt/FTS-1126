using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.ConveyorTelegramApiServer.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var initialLogger = InitialLogger.Create<Program>();
try
{
    var hostBuilder = ConveyorTelegramApiServerHostBuilder.CreateHostBuilder(args);
    var host = hostBuilder.Build();
    host.Run();
}
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
}