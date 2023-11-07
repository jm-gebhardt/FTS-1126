using System.Text.Json.Serialization;
using CommandLine;
using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.Common.Configurations.Extensions;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Logging;
using Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Exceptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Graph;
using Gebhardt.StoreWare.Agv.DeviceCommunication.States;
using MassTransit;
using MassTransit.Configuration;
using MassTransit.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                    }else if (simulationOptions.GridGraph)
                    {
                        services.AddTransient<IGraphGenerator, GridGraphGenerator >();
                    }
                    services.AddHostedService<SimulatedAgvProvider>();
                    services.ConfigureServicesMassTransit(context.Configuration,
                        x =>
                        {
                            x.AddDelayedMessageScheduler();
                            x.AddSagaStateMachine<SimulatedAgvStateMachine, SimulatedAgvSaga>()
                                .InMemoryRepository();
                        }, rabbitMqConfiguration: (_, cfg) =>
                        {
                            cfg.UseDelayedMessageScheduler();
                        });
                    services.AddOptions<MassTransitHostOptions>().Configure(options => { options.WaitUntilStarted = true; });
                });
        })
        .WithParsed<ProductionOptions>(productionOptions =>
        {
            hostBuilder = GebhardtStartup.CreateHostBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddScoped(_ => productionOptions);
                    services.AddSingleton<IAgvRegistry, AgvRegistry>();
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
                            configurator.AddConsumer<StartChargingCommandConsumer>();
                            configurator.AddConsumer<StopChargingCommandConsumer>();
                            configurator.AddConsumer<ActionEventConsumer>();
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
                });
        });

    // By using the generic overload of WithParsed above host will be null for invalid command line arguments.
    if (hostBuilder != null)
    {
        hostBuilder.ConfigureServices((_, services) =>
        {
            services.AddSingleton<IAgvRegistry, AgvRegistry>();

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        });

        var host = hostBuilder.Build();
        host.Run();
        return 0;
    }
    initialLogger.LogWarning("Invalid command line argument(s)");
    return 1;
}
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
    return 1;
}