using CommandLine;
using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Domain.Models.Sequence;
using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.States;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.BackgroundServices;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.StateMachines;
using MassTransit;
using MassTransit.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Configuration;

/// <summary>
/// Provides extensions methods to <see cref="IHostBuilder"/>.
/// </summary>
public static class HostBuilderExtension
{
    private static readonly TimeSpan _busStartTimeout = TimeSpan.FromSeconds(45);
    private const string QuartzSchedulerName = "Vda5050Simulator";
    private const string QuartzSchedulerId = "AUTO";
    private const string QuartzQueueName = "quartz-Vda5050Simulator";

    /// <summary>
    /// Extension block for <see cref="IHostBuilder"/>.
    /// </summary>
    extension(IHostBuilder hostBuilder)
    {
        /// <summary>
        /// Add MassTransit configuration.
        /// </summary>
        public IHostBuilder AddMassTransit(ILogger initialLogger)
        {
            hostBuilder.ConfigureServices((context, services) =>
            {
                try
                {
                    services.ConfigureServicesMassTransit(context.Configuration,
                        configurator =>
                        {
                            configurator.AddMqttConsumers();
                            configurator.AddConsumers();
                            configurator.AddSagaStateMachine<SimulatedAgvStateMachine, SimulatedAgv>(c =>
                                {
                                    c.Message<StartOrder>(m =>
                                    {
                                        m.UseMessageRetry(r =>
                                        {
                                            r.Interval(6, TimeSpan.FromSeconds(5));
                                            r.Handle<SagaStateMachineException>();
                                        });
                                    });
                                })
                                .InMemoryRepository();
                            configurator.AddMediator(c => c.AddMediator());
                            configurator.AddQuartzConsumers(options => options.QueueName = QuartzQueueName);
                        }
                        ,
                        // ReSharper disable once UnusedParameter.Local
                        (registrationContext, configurator) =>
                        {
                            //for outgoing MQTT messages the message topology needs to be configured
                            configurator.AddMqttMessages();

                            //for incoming MQTT messages the raw JSON serializer/deserializer needs to be added
                            var serializerFactory = new SystemTextJsonRawMessageSerializerFactory();
                            configurator.AddSerializer(serializerFactory, false);
                            configurator.UsePublishMessageScheduler();
                        });
                    services.AddOptions<MassTransitHostOptions>()
                        .Configure(options =>
                        {
                            options.WaitUntilStarted = true;
                            options.StartTimeout = _busStartTimeout;
                        });
                }
                catch (InvalidOperationException e)
                {
                    initialLogger.LogError(e, "MassTransit could not be started");
                    throw;
                }
            });
            return hostBuilder;
        }

        /// <summary>
        /// Add vda5050 simulator service
        /// </summary>
        public IHostBuilder AddSimulator(string[] args, ILogger initialLogger)
        {
            var parser = new Parser(cfg => cfg.CaseInsensitiveEnumValues = true);
            Options? simulationOptions = null;
            parser.ParseArguments<Options>(args)
                .WithParsed(o => { simulationOptions = o; });

            initialLogger.LogInformation("Simulating {NAgvs} agvs", simulationOptions?.Agvs);

            ConfigurationTypeManager.Instance.InitializeConfigurationTypes();
            hostBuilder.ConfigureServices((_, services) =>
            {
                services.AddSingleton(_ => simulationOptions!);
                services.ConfigureAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
                services.AddSingleton(TimeProvider.System);
                services.AddHostedService<AgvProvider>();
                services.AddQuartz(q =>
                {
                    q.SchedulerName = QuartzSchedulerName;
                    q.SchedulerId = QuartzSchedulerId;

                    // convert time zones using converter that can handle Windows/Linux differences
                    q.UseTimeZoneConverter();
                });
                services.AddSingleton<IAgvCache, AgvCache>();
                services.AddSingleton<IAgvStateCache, AgvStateCache>();
                services.AddTransient<INurbsEdgeBuilder, TinySplineEdgeBuilder>();
                // Sequence counters for topics wise header id incrementation for messages sent by the agv.
                services.AddSingleton(typeof(ISequenceCounter<>), typeof(SequenceCounter<>));
            });
            return hostBuilder;
        }
    }
}