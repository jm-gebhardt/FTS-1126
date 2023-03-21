using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommandLine;
using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Gebhardt.StoreWare.Agv.DeviceCommunication.States;
using MassTransit;
using MassTransit.Configuration;
using MassTransit.Serialization;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;
using Gebhardt.StoreWare.Agv.Common.Configurations.Extensions;

var initialLogger = Gebhardt.StoreWare.Agv.Common.Logging.InitialLogger.Create<Program>();
try
{
    IHost? host = null;

    Parser.Default.ParseArguments<SimulateOptions, ProductionOptions>(args)
        .WithParsed<SimulateOptions>(simulationOptions =>
        {
            // Enable serialization/deserialization of enum values as string.
            SystemTextJsonMessageSerializer.Options.Converters.Add(new JsonStringEnumConverter());

            host = GebhardtStartup.CreateHostBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddScoped(_ => simulationOptions);
                    services.AddTransient<SimulatedAgvProvider>();
                    services.AddSingleton<IAgvRegistry, AgvRegistry>();
                    services.ConfigureServicesMassTransit(context.Configuration,
                        x =>
                        {
                            x.AddDelayedMessageScheduler();
                            x.AddSagaStateMachine<SimulatedAgvStateMachine, SimulatedAgvSaga>()
                                .InMemoryRepository();
                            x.AddRequestClient<SimulatedAgvProvider>();
                            x.AddConsumer<SimulatedAgvProvider>();
                        }, rabbitMqConfiguration: (_, cfg) =>
                        {
                            cfg.UseDelayedMessageScheduler();
                        });
                })
                .Build();
        })
        .WithParsed<ProductionOptions>(productionOptions =>
        {
            host = GebhardtStartup.CreateHostBuilder(args)
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
                            configurator.AddConsumer<StartChargingCommandConsumer>();
                            configurator.AddConsumer<StopChargingCommandConsumer>();
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
                })
                .Build();
        });

    // By using the generic overload of WithParsed above host will be null for invalid command line arguments.
    if (host != null)
    {
        host.Run();
        return 0;
    }
    else
    {
        initialLogger.LogWarning("Invalid command line argument(s)");
        return 1;
    }
}
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
    return 1;
}