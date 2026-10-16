using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.Vda5050Simulator.Configuration;

var initialLogger = InitialLogger.Create<Program>();
try
{
		var host = Vda5050SimulatorHostBuilder.CreateHostBuilder(args).Build();
		host.Run();
		return 0;
}
catch (Exception ex)
{
		initialLogger.LogError(ex, "Unhandled exception");
		return 1;
}
