using FluentValidation;
using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Domain.Models.Sequence;
using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Sagas;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order.OrderAction;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Filters;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.BackgroundServices;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.StateMachines;
using MassTransit;
using MassTransit.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Configuration;

/// <summary>
/// Provides extensions methods to <see cref="IHostBuilder"/>.
/// </summary>
public static class HostBuilderExtension
{
    private static readonly TimeSpan _busStartTimeout = TimeSpan.FromSeconds(45);

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
                        busRegistrationConfiguration: configurator =>
                        {
                            configurator.AddMqttConsumers();
                            configurator.AddConsumers();
                            configurator.AddSagaStateMachine<OrderExecutionStateMachine, OrderExecutionSaga>()
                                .InMemoryRepository();
                            configurator.AddMediator(c => c.AddMediator());
                        }
                        ,
                        // ReSharper disable once UnusedParameter.Local
                        rabbitMqConfiguration: (registrationContext, configurator) =>
                        {
                            //for outgoing MQTT messages the message topology needs to be configured
                            configurator.AddMqttMessages();

                            configurator.AddFilters(registrationContext);

                            //for incoming MQTT messages the raw JSON serializer/deserializer needs to be added
                            var serializerFactory = new SystemTextJsonRawMessageSerializerFactory();
                            configurator.AddSerializer(serializerFactory, false);
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
        /// Add DeviceCommunication.Vda5050 service
        /// </summary>
        public IHostBuilder AddDeviceCommunicationVda5050()
        {
            ConfigurationTypeManager.Instance.InitializeConfigurationTypes();
            hostBuilder.ConfigureServices((context, services) =>
            {
                services.ConfigureTracing(context.Configuration);
                services.AddDbContextFactory<DeviceCommunicationVda5050DbContext>(builder =>
                {
                    string? connectionString = context.Configuration.GetConnectionString(DeviceCommunicationVda5050DbContext.ConfigSectionName);
                    builder.UseSqlServer(connectionString, m =>
                    {
                        m.MigrationsHistoryTable(IDbContext<DeviceCommunicationVda5050DbContext>.MigrationTableName);
                        m.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    });
                    builder.AddInterceptors(new EfCommandMetricsInterceptor());
                });
                services.ConfigureAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

                //Repositories
                services.AddTransient<IAgvDataRepository, EfAgvDataRepository>();
                services.AddTransient<IAgvActionRepository, EfAgvActionRepository>();
                services.AddTransient<IAgvRepository, EfAgvRepository>();
                services.AddTransient<ILifRepository, EfLifRepository>();
                services.AddTransient<IAgvTypeRepository, EfAgvTypeRepository>();

                //Services
                services.AddHostedService<InitializationProvider>();
                services.AddSingleton(TimeProvider.System);
                services.AddTransient<INurbsEdgeBuilder, TinySplineEdgeBuilder>();
                services.AddValidatorsFromAssemblyContaining<ILifRepository>();

                //Caches
                services.AddSingleton<IAgvCacheProvider, AgvCache>();
                services.AddSingleton<IFactsheetRequestCacheProvider, FactsheetRequestCache>();
                services.AddSingleton<IOrderActionStatusCacheProvider, OrderActionStatusCache>();

                //Filter
                services.AddSingleton<ISemaphoreProvider, SemaphoreProvider>();
                services.AddSingleton<ISignificanceChecker<MqttState>, MqttStateSignificanceChecker>();
                services.AddSingleton<UnTimedInMemoryFilter<MqttState>>();

                // Order action builder
                // Predefined VDA5050 order actions
                services.AddTransient<IOrderActionBuilder, DetectObjectOrderActionBuilder>();
                services.AddTransient<IOrderActionBuilder, DropOrderActionBuilder>();
                services.AddTransient<IOrderActionBuilder, EnableMapOrderActionBuilder>();
                services.AddTransient<IOrderActionBuilder, FinePositioningOrderActionBuilder>();
                services.AddTransient<IOrderActionBuilder, InitPositionOrderActionBuilder>();
                services.AddTransient<IOrderActionBuilder, PickOrderActionBuilder>();
                services.AddTransient<IOrderActionBuilder, StartChargingOrderActionBuilder>();
                services.AddTransient<IOrderActionBuilder, StopChargingOrderActionBuilder>();
                services.AddTransient<IOrderActionBuilder, WaitForTriggerOrderActionBuilder>();

                services.AddTransient<OrderActionBuilderRegistry>();
                services.AddTransient<IOrderBuilder, OrderBuilder>();

                // Sequence counters for topics wise header id incrementation for messages sent to the agv.
                services.AddSingleton(typeof(ISequenceCounter<>), typeof(SequenceCounter<>));
            });
            return hostBuilder;
        }
    }
}