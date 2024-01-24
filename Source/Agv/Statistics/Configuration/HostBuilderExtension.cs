using Gebhardt.StoreWare.Agv.Common.Configurations;
using Gebhardt.StoreWare.Agv.Common.Configurations.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.Filters;
using Gebhardt.StoreWare.Agv.Statistics.Services.BackgroundServices;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Statistics.Domain.Repositories;
using Statistics.Persistence.Ef.DataAccess;
using Statistics.Persistence.Ef.Repositories;

namespace Gebhardt.StoreWare.Agv.Statistics.Configuration;

/// <summary>
/// Provides extension method for <see cref="IHostBuilder"/>.
/// </summary>
public static class HostBuilderExtension
{
    /// <summary>
    /// Add MassTransit configuration.
    /// </summary>
    public static IHostBuilder AddMassTransit(this IHostBuilder hostBuilder, ILogger initialLogger)
    {
        hostBuilder.ConfigureServices((context, services) =>
        {
            try
            {
                services.AddHostedService<InitialGridProvider>();
                services.ConfigureServicesMassTransit(context.Configuration, x =>
                {
                    x.AddBusConsumers();

                    x.AddMediator(c => c.AddMediatorConsumers());
                }, rabbitMqConfiguration: (ctx, cfg) =>
                {
                    cfg.UseConsumeFilter<AgvTracingConsumeFilters>(ctx);
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
    /// Add service specific configurations.
    /// </summary>
    public static IHostBuilder AddStatistics(this IHostBuilder hostBuilder)
    {
        ConfigurationTypeManager.Instance.InitializeConfigurationTypes();

        hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddConfigurationTypeManager();
            services.AddDbContext<StatisticsDbContext>(o =>
                o.UseSqlServer(context.Configuration.GetConnectionString(StatisticsDbContext.ConfigSectionName)));
            services.AddTransient<IStatisticsDb, EfStatisticsDb>();
            services.AddTransient<IMapRepository, EfMapRepository>();
            services.AddTransient<IGridRepository, EfGridRepository>();
            services.AddTransient<IAgvGridPositionRepository, EfAgvGridPositionRepository>();
        });
        return hostBuilder;
    }
}