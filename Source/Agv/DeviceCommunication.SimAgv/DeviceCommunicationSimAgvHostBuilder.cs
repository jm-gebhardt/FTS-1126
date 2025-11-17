using System.Text.Json.Serialization;
using CommandLine;
using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Configurations.SettingsProviders;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.DeviceCommunication.Common.Services;
using Gebhardt.Acc.DeviceCommunication.SimAgv.CmdOptions;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Configuration;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Exceptions;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Maps;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Speed;
using Gebhardt.Acc.DeviceCommunication.SimAgv.States;
using MassTransit;
using MassTransit.Serialization;
using Prometheus;
using Quartz;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv;

/// <summary>
/// Contains the host building logic for DeviceCommunication SimAgv.
/// </summary>
public static class DeviceCommunicationSimAgvHostBuilder
{
    /// <summary>
    /// Creates the host builder.
    /// </summary>
    public static IHostBuilder? CreateHostBuilder(string[] args, string karisExportSection, string exportDir, string quartzSchedulerName, string quartzSchedulerId, string quartzQueueName)
    {
        IHostBuilder? hostBuilder1 = null;
        var parser = new Parser(cfg => cfg.CaseInsensitiveEnumValues = true);
        parser.ParseArguments<Options>(args)
            .WithParsed(simulationOptions =>
            {
                if (simulationOptions is { FixedGraph: false, KaeserGraph: false, GridGraph: false, BezierGridGraph: false, ExportedGraph: false })
                {
                    throw new GraphGeneratorConfigurationException($"No graph generator selected (GridGraph: {simulationOptions.GridGraph}/ FixedGraph {simulationOptions.FixedGraph} / KaeserGraph {simulationOptions.KaeserGraph} / BezierGraph {simulationOptions.BezierGridGraph})");
                }

                if (!SystemTextJsonMessageSerializer.Options.Converters.IsReadOnly)
                {
                    // Enable serialization/deserialization of enum values as string.
                    SystemTextJsonMessageSerializer.Options.Converters.Add(new JsonStringEnumConverter());
                }

                hostBuilder1 = GebhardtStartup.CreateHostBuilder(args)
                    .ConfigureServices((context, services) =>
                    {
                        services.AddSingleton(_ => simulationOptions);
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
                        else if (simulationOptions.BezierGridGraph)
                        {
                            services.AddSingleton<IGraphGenerator, BezierGraphGenerator>();
                            services.AddTransient<IMapGenerator, GeneratedMapGenerator>();
                        }
                        else if (simulationOptions.KaeserGraph)
                        {
                            services.AddTransient<IGraphGenerator, KaeserGraphGenerator>();
                            services.AddTransient<IMapGenerator, GeneratedMapGenerator>();
                        }
                        else if (simulationOptions.ExportedGraph)
                        {
                            string? exportDirectory = context.Configuration.GetSection(karisExportSection)[exportDir];
                            var config = new KarisExportConfiguration(exportDirectory);
                            config.LoadFromYaml();
                            services.AddSingleton(config);
                            services.AddTransient<IGraphReader, JsonGraphReader>();
                            services.AddTransient<IGraphGenerator, ExportedGraphGenerator>();
                            services.AddTransient<IMapGenerator, ExportedGraphMapGenerator>();
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
                                configurator.AddConsumer<SetSimulationSpeedConsumer>();
                            }, rabbitMqConfiguration: (_, cfg) => { cfg.UsePublishMessageScheduler(); });
                        services.AddOptions<MassTransitHostOptions>().Configure(options => { options.WaitUntilStarted = true; });

                        services.AddHealthChecks();
                    }).ConfigureWebHost(host =>
                    {
                        host.UseKestrel()
                            .ConfigureAppConfiguration((_, config) =>
                            {
                                ConfigurationTypeManager.Instance.InitializeConfigurationTypes();
                                config.Sources.Insert(0, new DefaultConfigurationSource(ConfigurationTypeManager.Instance));
                            })
                            .AddDeviceCommunicationWebApi()
                            .Configure(app =>
                            {
                                app.UseMetricServer();
                                app.UseDeviceCommunicationWebApi();
                                app.UseDeveloperExceptionPage();
                            });
                    });
            });
        return hostBuilder1;
    }
}