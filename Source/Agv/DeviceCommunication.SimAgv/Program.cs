using CommandLine;
using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.DeviceCommunication.Common.Repositories;
using Gebhardt.Acc.DeviceCommunication.SimAgv;
using Gebhardt.Acc.DeviceCommunication.SimAgv.CmdOptions;

const string quartzSchedulerName = "MassTransit-DeviceCommunication";
const string quartzSchedulerId = "AUTO";
const string quartzQueueName = "quartz-DeviceCommunication";
const string karisExportSection = "KARISEXPORT";
const string exportDir = "DIRECTORY";

var initialLogger = InitialLogger.Create<Program>();

try
{
    var parser = new Parser(cfg => cfg.CaseInsensitiveEnumValues = true);
    Options? simulationOptions = null;
    parser.ParseArguments<Options>(args)
        .WithParsed(o => { simulationOptions = o; });

    var hostBuilder = DeviceCommunicationSimAgvHostBuilder.CreateHostBuilder(args, simulationOptions, karisExportSection, exportDir, quartzSchedulerName, quartzSchedulerId, quartzQueueName);

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
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
    return 1;
}