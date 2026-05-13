using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var initialLogger = InitialLogger.Create<Program>();
try
{
    var hostBuilder = DeviceCommunicationVda5050SimulatorHostBuilder.CreateHostBuilder(args, initialLogger);
    var host = hostBuilder.Build();
   
    host.Run();
    return 0;
}
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
    return 1;
}
