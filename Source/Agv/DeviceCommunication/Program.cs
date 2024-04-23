using System.Text.Json.Serialization;
using CommandLine;
using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.Common.Time;
using Gebhardt.Acc.DeviceCommunication.CmdOptions;
using Gebhardt.Acc.DeviceCommunication.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Messages.MqttMessages;
using Gebhardt.Acc.DeviceCommunication.Services;
using Gebhardt.Acc.DeviceCommunication.Services.Graphs;
using Gebhardt.Acc.DeviceCommunication.Services.Simulation;
using Gebhardt.Acc.DeviceCommunication.Services.Simulation.Graphs;
using Gebhardt.Acc.DeviceCommunication.Services.Simulation.Maps;
using Gebhardt.Acc.DeviceCommunication.States;
using Gebhardt.Acc.DeviceCommunication.WebApi;
using MassTransit;
using MassTransit.Configuration;
using MassTransit.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    parser.ParseArguments<SimulateOptions, ProductionOptions>(args)
        .WithParsed<SimulateOptions>(simulationOptions =>
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
                            configurator.AddMqttConsumer<SimulateAgvErrorConsumer>();
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
        })
        .WithParsed<ProductionOptions>(productionOptions =>
        {
            hostBuilder = GebhardtStartup.CreateHostBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddScoped(_ => productionOptions);
                    services.ConfigureServicesMassTransit(context.Configuration,
                        busRegistrationConfiguration: configurator =>
                        {
                            //add consumers here
                            configurator.AddMqttConsumer<AgvStateConsumer>();
                            configurator.AddMqttConsumer<AgvDataConsumer>();
                            configurator.AddMqttConsumer<AgvGraphConsumer>();
                            configurator.AddMqttConsumer<AgvStationListConsumer>();
                            configurator.AddMqttConsumer<OrderEventConsumer>();
                            configurator.AddMqttConsumer<MqttMapConsumer>();
                            configurator.AddSagaStateMachine<AgvGraphBuilderStateMachine, AgvGraphBuilderSaga>().InMemoryRepository();
                            configurator.AddConsumer<StartActionCommandConsumer>();
                            configurator.AddConsumer<RequestAgvMapConsumer>();
                            configurator.AddConsumer<AgvRequestDataConsumer>();
                            configurator.AddConsumer<ActionEventConsumer>();
                            configurator.AddConsumer<GraphMerger>();
                        },

                        // ReSharper disable once UnusedParameter.Local
                        rabbitMqConfiguration: (registrationContext, configurator) =>
                        {
                            //for outgoing MQTT messages the message topology needs to be configured
                            configurator.ConfigureMessageTopology<MqttRequestData>();
                            configurator.ConfigureMessageTopology<MqttRequestGraph>();
                            configurator.ConfigureMessageTopology<MqttOrder>();
                            configurator.ConfigureMessageTopology<MqttRequestStationList>();
                            configurator.ConfigureMessageTopology<MqttRequestMap>();

                            //for incoming MQTT messages the raw JSON serializer/deserializer needs to be added
                            var serializerFactory = new SystemTextJsonRawMessageSerializerFactory();
                            configurator.AddSerializer(serializerFactory, false);
                        });
                }).ConfigureWebHost(host =>
                {
                    host.UseKestrel()
                        .Configure(app =>
                        {
                            app.UseMetricServer();
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