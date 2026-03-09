using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Filters;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.Repositories;
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
                });

                services.AddTransient<IAgvDataRepository, EfAgvDataRepository>();
                services.AddTransient<IAgvActionRepository, EfAgvActionRepository>();
                services.AddSingleton<ISemaphoreProvider, SemaphoreProvider>();
                services.AddSingleton<ISignificanceChecker<MqttState>, MqttStateSignificanceChecker>();
                services.AddSingleton<UnTimedInMemoryFilter<MqttState>>();

                services.AddAutoMapper(_ => { }, AppDomain.CurrentDomain.GetAssemblies());
            });
            return hostBuilder;
        }
    }
}