using CommandLine;
using Gebhardt.Acc.Common;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Common.Time;
using Gebhardt.Acc.DeviceCommunication.Common.Configuration;
using Gebhardt.Acc.DeviceCommunication.Common.Repositories;
using Gebhardt.Acc.DeviceCommunication.Common.Services;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.CmdOptions;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Filters;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.DataAccess;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.States;
using MassTransit;
using MassTransit.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using IAgvRepository = Gebhardt.Acc.DeviceCommunication.Common.Repositories.IAgvRepository;

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
                    services.AddSingleton<IConfigurationReader, InstantActionsConfigurationFileReader>();
                    services.AddHostedService<InitialCacheProvider>();

                    void ConfigureDbContextOptions(DbContextOptionsBuilder builder)
                    {
                        string? connectionString = context.Configuration.GetConnectionString(DeviceCommunicationDbContext.ConfigSectionName);
                        builder.UseSqlServer(connectionString, m =>
                        {
                            m.MigrationsHistoryTable(DeviceCommunicationDbContext.MigrationTableName);
                            m.UseCompatibilityLevel(120); // EF9 compatibility level
                        });
                    }

                    services.AddDbContext<DeviceCommunicationDbContext>(ConfigureDbContextOptions);

                    services.AddDbContextFactory<DeviceCommunicationDbContext>(ConfigureDbContextOptions);

                    services.AddSingleton(typeof(ISemaphoreProvider<>), typeof(SemaphoreProvider<>));

                    services.AddTransient<Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories.IAgvRepository, EfAgvRepository>();
                    services.AddSingleton<IAgvCacheProvider, AgvCache>();
                    services.AddSingleton<IMqttEdgeMappingCacheProvider, MqttEdgeMappingCache>();
                    services.AddSingleton<IMqttNodeCacheProvider, MqttNodeCache>();

                    // Filters
                    services.AddSingleton<ISemaphoreProvider, SemaphoreProvider>();
                    services.AddSingleton<ISignificanceChecker<MqttOrderEvent>, MqttOrderEventSignificanceChecker>();
                    services.AddSingleton<UnTimedInMemoryFilter<MqttOrderEvent>>();

                    services.AddSingleton<ISignificanceChecker<MqttState>, MqttStatusSignificanceChecker>();
                    services.AddSingleton<UnTimedInMemoryFilter<MqttState>>();

                    services.AddSingleton<ISignificanceChecker<ActionCompleted>, ActionEventSignificanceChecker<ActionCompleted>>();
                    services.AddSingleton<UnTimedInMemoryFilter<ActionCompleted>>();
                   
                    services.AddSingleton<ISignificanceChecker<ActionCanceled>, ActionEventSignificanceChecker<ActionCanceled>>();
                    services.AddSingleton<UnTimedInMemoryFilter<ActionCanceled>>();

                    services.AddSingleton<ISignificanceChecker<ActionStarted>, AgvActionSignificanceChecker<ActionStarted>>();
                    services.AddSingleton<UnTimedInMemoryFilter<ActionStarted>>();
                    
                    services.AddSingleton<ISignificanceChecker<ActionUpdated>, AgvActionSignificanceChecker<ActionUpdated>>();
                    services.AddSingleton<UnTimedInMemoryFilter<ActionUpdated>>();

                    services.AddSingleton<ISignificanceChecker<AgvSetToPushOrJoystickMode>, PushModeSignificanceChecker<AgvSetToPushOrJoystickMode>>();
                    services.AddSingleton<UnTimedInMemoryFilter<AgvSetToPushOrJoystickMode>>();

                    services.AddTransient<IActionRepository, EfActionRepository>();
                    services.ConfigureServicesMassTransit(context.Configuration,
                        busRegistrationConfiguration: configurator =>
                        {
                            //add consumers here
                            configurator.AddMqttConsumer<MqttLastWillConsumer>();
                            configurator.AddMqttConsumer<MqttStateConsumer>();
                            configurator.AddMqttConsumer<AgvDataConsumer>();
                            configurator.AddMqttConsumer<MqttGraphConsumer>();
                            configurator.AddMqttConsumer<MqttZonesConsumer>();
                            configurator.AddMqttConsumer<MqttStationListConsumer>();
                            configurator.AddMqttConsumer<MqttOrderEventConsumer>();
                            configurator.AddMqttConsumer<MqttMapConsumer>();
                            configurator.AddSagaStateMachine<AgvGraphBuilderStateMachine, AgvGraphBuilderSaga>().InMemoryRepository();
                            configurator.AddConsumer<ActionCommandConsumer>();
                            configurator.AddConsumer<InstantActionsConsumer>();
                            configurator.AddConsumer<RequestAgvMapConsumer>();
                            configurator.AddConsumer<AgvZonesConsumer>();
                            configurator.AddConsumer<RequestAgvDataConsumer>();
                            configurator.AddConsumer<ActionAbortedConsumer>();
                            configurator.AddConsumer<GraphMerger>();
                            configurator.AddConsumer<AgvStationConsumer>();
                        },

                        // ReSharper disable once UnusedParameter.Local
                        rabbitMqConfiguration: (registrationContext, configurator) =>
                        {
                            //for outgoing MQTT messages the message topology needs to be configured
                            configurator.ConfigureMessageTopology<MqttRequestData>();
                            configurator.ConfigureMessageTopology<MqttRequestGraph>();
                            configurator.ConfigureMessageTopology<MqttOrder>();
                            configurator.ConfigureMessageTopology<MqttZones>();
                            configurator.ConfigureMessageTopology<MqttRequestStationList>();
                            configurator.ConfigureMessageTopology<MqttRequestMap>();
                            configurator.ConfigureMessageTopology<MqttInstantActions>();
                            configurator.ConfigureMessageTopology<MqttRequestZones>();
                            configurator.ConfigureMessageTopology<MqttRequestZoneRemoveAgv>();
                            configurator.ConfigureMessageTopology<MqttRequestStationRemoveAgv>();

                            // add filters
                            configurator.UseConsumeFilter<MqttMessageConsumeFilter>(registrationContext);
                            configurator.UsePublishFilter<ActionEventPublishFilter>(registrationContext);
                            configurator.UsePublishFilter<PushModePublishFilter>(registrationContext);

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
            services.AddSingleton<IGraphRepository, EfGraphRepository>();
            services.AddSingleton<ITimeProvider, DateTimeNowUtcProvider>();

            services.AddAutoMapper(_ => { }, AppDomain.CurrentDomain.GetAssemblies());
        });

        var host = hostBuilder.Build();
        using (var scope = host.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DeviceCommunicationDbContext>();
            await dbContext.Database.MigrateAsync();
        }
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
