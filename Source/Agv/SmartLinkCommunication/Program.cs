using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.SmartLinkCommunication;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var initialLogger = InitialLogger.Create<Program>();

try
{
    var config = await CmdParser.ParseAsync(args, initialLogger);

    if (config == null)
    {
        initialLogger.LogError("Invalid command line argument(s)");
        return 1;
    }

    var hostBuilder = SmartLinkHostBuilder.CreateHostBuilder(args, config);

    var host = hostBuilder.Build();

    await host.RunAsync();
    return 0;
}
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
    return 1;
}