using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.TestPlanRunner.Configuration;

var initialLogger = InitialLogger.Create<Program>();

try
{
    var hostBuilder = TestPlanRunnerHostBuilder.CreateHostBuilder(args, initialLogger);
    var host = hostBuilder.Build();
    host.Run();
    return 0;
}
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
    return 1;
}
