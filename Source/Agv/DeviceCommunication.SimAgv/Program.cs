using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.DeviceCommunication.Common.Repositories;
using Gebhardt.Acc.DeviceCommunication.SimAgv;

const string quartzSchedulerName = "MassTransit-DeviceCommunication";
const string quartzSchedulerId = "AUTO";
const string quartzQueueName = "quartz-DeviceCommunication";
const string karisExportSection = "KARISEXPORT";
const string exportDir = "DIRECTORY";

var initialLogger = InitialLogger.Create<Program>();

try
{
    var hostBuilder = DeviceCommunicationSimAgvHostBuilder.CreateHostBuilder(args, karisExportSection, exportDir, quartzSchedulerName, quartzSchedulerId, quartzQueueName);

    // By using the generic overload of WithParsed above host will be null for invalid command line arguments.
    if (hostBuilder != null)
    {
        hostBuilder.ConfigureServices((_, services) =>
        {
            services.AddSingleton<IAgvRepository, AgvRepository>();
            services.AddSingleton(TimeProvider.System);

            services.AddAutoMapper(_ => { }, AppDomain.CurrentDomain.GetAssemblies());
        });

        var host = hostBuilder.Build();
        host.Run();
        return 0;
    }
    initialLogger.LogError("Invalid command line argument(s)");
    return 1;
}
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
    return 1;
}
