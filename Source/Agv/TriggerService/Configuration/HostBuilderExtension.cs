using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Messages.Filters;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TriggerService.Domain.Repositories;
using Gebhardt.Acc.TriggerService.Domain.Services;
using Gebhardt.Acc.TriggerService.Persistence.Ef.DataAccess;
using Gebhardt.Acc.TriggerService.Persistence.Ef.Repositories;
using Gebhardt.Acc.TriggerService.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TriggerService.Configuration;

/// <summary>
/// Provides extension method for <see cref="IHostBuilder"/>.
/// </summary>
public static class HostBuilderExtension
{
    private const string TriggerConfigSection = "TRIGGER";

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
                services.ConfigureServicesMassTransit(context.Configuration, x =>
                {
                    x.AddBusConsumers();

                    x.AddMediator(c => c.AddMediatorConsumers());
                }, rabbitMqConfiguration: (ctx, cfg) => { cfg.UseConsumeFilter<AgvTracingConsumeFilters>(ctx); });
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
        /// Add service specific configurations.
        /// </summary>
        public IHostBuilder AddTriggerService(ILogger initialLogger)
    {
        hostBuilder.ConfigureServices((context, services) =>
        {
            var config = services.AddTriggerServiceConfiguration(context.Configuration);
            services.RegisterTriggerDependencies(initialLogger, config);
            services.AddHostedService<TriggerConfigurator>();
            services.AddSingleton<ITriggerManager, TriggerManager>();
            services.AddTransient<ReflectionService>();
            services.AddSingleton<ITriggerRepository, EfTriggerRepository>();
            // -  database
            services.AddDbContextFactory<TriggerServiceDbContext>(builder =>
            {
                string? connectionString = context.Configuration.GetConnectionString(TriggerServiceDbContext.ConfigSectionName);

                builder.UseSqlServer(connectionString, m =>
                {
                    m.MigrationsHistoryTable(IDbContext<TriggerServiceDbContext>.MigrationTableName);
                    m.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });
            });
        });
        return hostBuilder;
    }
    }

    /// <summary>
    /// Extension block for <see cref="IServiceCollection"/>.
    /// </summary>
    extension(IServiceCollection services)
    {
        private TriggerServiceConfiguration AddTriggerServiceConfiguration(IConfiguration configuration)
        {
            var triggerServiceConfiguration = new TriggerServiceConfiguration();
            triggerServiceConfiguration.UpdateFromConfigurationSection(configuration.GetRequiredSection(TriggerConfigSection));
            services.AddSingleton(triggerServiceConfiguration);
            return triggerServiceConfiguration;
        }
    }
}