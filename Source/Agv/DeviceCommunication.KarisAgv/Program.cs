using CommandLine;
using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.Common.Time;
using Gebhardt.Acc.DeviceCommunication.Common.Repositories;
using Gebhardt.Acc.DeviceCommunication.Common.Services;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.CmdOptions;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.States;
using MassTransit;
using MassTransit.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

var initialLogger = InitialLogger.Create<Program>();

try
{
    IHostBuilder? hostBuilder = null;
    var parser = new Parser(cfg => cfg.CaseInsensitiveEnumValues = true);
    parser.ParseArguments<Options>(args)
        .WithParsed(productionOptions =>
        {
            hostBuilder = GebhardtStartup.CreateHostBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddScoped(_ => productionOptions);
                    services.ConfigureServicesMassTransit(context.Configuration,
                        busRegistrationConfiguration: configurator =>
                        {
                            //add consumers here
                            configurator.AddMqttConsumer<MqttStateConsumer>();
                            configurator.AddMqttConsumer<AgvDataConsumer>();
                            configurator.AddMqttConsumer<MqttGraphConsumer>();
                            configurator.AddMqttConsumer<MqttStationListConsumer>();
                            configurator.AddMqttConsumer<MqttOrderEventConsumer>();
                            configurator.AddMqttConsumer<MqttMapConsumer>();
                            configurator.AddSagaStateMachine<AgvGraphBuilderStateMachine, AgvGraphBuilderSaga>().InMemoryRepository();
                            configurator.AddConsumer<StartActionCommandConsumer>();
                            configurator.AddConsumer<RequestAgvMapConsumer>();
                            configurator.AddConsumer<RequestAgvDataConsumer>();
                            configurator.AddConsumer<ActionAbortedConsumer>();
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
            services.AddSingleton<IGraphRepository, GraphRepository>();
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