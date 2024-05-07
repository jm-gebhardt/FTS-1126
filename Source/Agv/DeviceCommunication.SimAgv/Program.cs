using System.Text.Json.Serialization;
using CommandLine;
using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.Common.Time;
using Gebhardt.Acc.DeviceCommunication.Common.Repositories;
using Gebhardt.Acc.DeviceCommunication.Common.Services;
using Gebhardt.Acc.DeviceCommunication.SimAgv;
using Gebhardt.Acc.DeviceCommunication.SimAgv.CmdOptions;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Exceptions;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Maps;
using Gebhardt.Acc.DeviceCommunication.SimAgv.States;
using MassTransit;
using MassTransit.Serialization;
using Quartz;
using Prometheus;

const string quartzSchedulerName = "MassTransit-DeviceCommunication";
const string quartzSchedulerId = "AUTO";
const string quartzQueueName = "quartz-DeviceCommunication";

var initialLogger = InitialLogger.Create<Program>();

try
{
    IHostBuilder? hostBuilder = null;
    var parser = new Parser(cfg => cfg.CaseInsensitiveEnumValues = true);
    parser.ParseArguments<Options>(args)
        .WithParsed(simulationOptions =>
        {
            if (!simulationOptions.GridGraph && !simulationOptions.FixedGraph)
            {
                throw new GraphGeneratorConfigurationException($"No graph generator selected (GridGraph: {simulationOptions.GridGraph}/ FixedGraph {simulationOptions.FixedGraph})");
            }
            // Enable serialization/deserialization of enum values as string.
            SystemTextJsonMessageSerializer.Options.Converters.Add(new JsonStringEnumConverter());

            hostBuilder = GebhardtStartup.CreateHostBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddScoped(_ => simulationOptions);
                    if (simulationOptions.FixedGraph)
                    {
                        services.AddTransient<IGraphGenerator, FixedGraphGenerator>();
                        services.AddTransient<IMapGenerator, FixedGraphMapGenerator>();
                    }
                    else if (simulationOptions.GridGraph)
                    {
                        services.AddTransient<IGraphGenerator, GridGraphGenerator>();
                        services.AddTransient<IMapGenerator, GeneratedMapGenerator>();
                    }
                    services.AddHostedService<SimulatedAgvProvider>();

                    services.AddQuartz(q =>
                    {
                        q.SchedulerName = quartzSchedulerName;
                        q.SchedulerId = quartzSchedulerId;

                        // convert time zones using converter that can handle Windows/Linux differences
                        q.UseTimeZoneConverter();
                    });

                    services.ConfigureServicesMassTransit(context.Configuration,
                        configurator =>
                        {
                            configurator.AddPublishMessageScheduler();

                            configurator.AddQuartzConsumers(options => options.QueueName = quartzQueueName);

                            configurator.AddSagaStateMachine<SimulatedAgvStateMachine, SimulatedAgvSaga>()
                                .InMemoryRepository();
                            configurator.AddMqttConsumer<MqttSimulateAgvErrorConsumer>();
                            configurator.AddConsumer<SimulatedAgvProvider>();
                        }, rabbitMqConfiguration: (_, cfg) => { cfg.UsePublishMessageScheduler(); });
                    services.AddOptions<MassTransitHostOptions>().Configure(options => { options.WaitUntilStarted = true; });
                }).ConfigureWebHost(host =>
                {
                    host.UseKestrel()
                        .AddDeviceCommunicationWebApi()
                        .Configure(app =>
                        {
                            app.UseMetricServer();
                            app.UseDeviceCommunicationWebApi();
                            app.UseDeveloperExceptionPage();
                        });
                });
        });

    // By using the generic overload of WithParsed above host will be null for invalid command line arguments.
    if (hostBuilder != null)
    {
        hostBuilder.ConfigureServices((_, services) =>
        {
            services.AddSingleton<IAgvRepository, AgvRepository>();
            services.AddSingleton<ITimeProvider, DateTimeNowUtcProvider>();

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
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