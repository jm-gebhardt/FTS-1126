using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.OrderTranslator.Domain.Services;
using Gebhardt.Acc.OrderTranslator.Persistence.Ef.DataAccess;
using Gebhardt.Acc.OrderTranslator.Persistence.Ef.Repositories;
using Gebhardt.Acc.OrderTranslator.Services.BackgroundServices;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IHostOrderRepository = Gebhardt.Acc.OrderTranslator.Domain.Repositories.IHostOrderRepository;
using IStationRepository = Gebhardt.Acc.OrderTranslator.Domain.Repositories.IStationRepository;

namespace Gebhardt.Acc.OrderTranslator.Configuration;

/// <summary>
/// Provides extension method for <see cref="IHostBuilder"/>.
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
                    services.AddSingleton(TimeProvider.System);
                    //Configure masstransit
                    services.ConfigureServicesMassTransit(context.Configuration, x => { x.AddBusConsumers(); });
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
        /// Add service specific configurations
        /// </summary>
        public IHostBuilder AddOrderTranslator()
        {
            ConfigurationTypeManager.Instance.InitializeConfigurationTypes();
            hostBuilder.ConfigureServices((context, services) =>
            {
                services.AddConfigurationTypeManager();
                services.AddSingleton(typeof(ISemaphoreProvider<>), typeof(SemaphoreProvider<>));

                services.AddSingleton<StartupHealthCheck>();
                services.AddTransient<IHostOrderRepository, EfHostOrderRepository>();
                services.AddHostedService<InitializationProvider>();

                services.AddDbContextFactory<OrderTranslatorDbContext>(builder =>
                {
                    string? connectionString = context.Configuration.GetConnectionString(OrderTranslatorDbContext.ConfigSectionName);
                    builder.UseSqlServer(connectionString, m =>
                    {
                        m.MigrationsHistoryTable(IDbContext<OrderTranslatorDbContext>.MigrationTableName);
                        m.UseCompatibilityLevel(120); // EF9 compatibility level
                    });
                });
                services.AddSingleton<IStationRepository, EfStationRepository>();
                services.TryAddStationConfiguration();
                services.RegisterValidators();
            });
            return hostBuilder;
        }
    }
}