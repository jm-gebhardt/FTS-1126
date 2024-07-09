using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Messages.Filters;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.PrismatCommunication.CmdOptions;
using Gebhardt.Acc.Common.Time;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Services;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;
using Gebhardt.Acc.PrismatCommunication.Persistence.Repositories;
using Gebhardt.Acc.PrismatCommunication.Services.TelegramApi;
using Gebhardt.Acc.PrismatCommunication.SimulationMode;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Configuration;

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
                services.AddSingleton<ITimeProvider, DateTimeNowUtcProvider>();
                services.ConfigureServicesMassTransit(context.Configuration, x =>
                {
                    x.AddBusConsumers();
                }, rabbitMqConfiguration: (ctx, cfg) =>
                {
                    cfg.UseConsumeFilter<ActionGroupTracingConsumeFilters>(ctx);
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
    public static IHostBuilder AddPrismatCommunication(this IHostBuilder hostBuilder)
    {
        return hostBuilder.AddPrismatCommunication(null);
    }

    /// <summary>
    /// Add service specific configurations.
    /// </summary>
    /// <param name="hostBuilder">This <see cref="IHostBuilder"/>.</param>
    /// <param name="simulateOptions">The <see cref="SimulateOptions"/> if in simulation mode.</param>
    public static IHostBuilder AddPrismatCommunication(this IHostBuilder hostBuilder, SimulateOptions? simulateOptions)
    {
        ConfigurationTypeManager.Instance.InitializeConfigurationTypes();

        hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddConfigurationTypeManager();
            services.AddDbContext<PrismatCommunicationDbContext>(o =>
                o.UseSqlServer(context.Configuration.GetConnectionString(PrismatCommunicationDbContext.ConfigSectionName)));
            services.AddTransient<IHostOrderRepository, EfHostOrderRepository>();
            services.TryAddKaeserGraphConfiguration(simulateOptions != null);

            if (simulateOptions == null)
            {
                services.TryAddPrismatApi(context.Configuration);
                services.AddSingleton<IHostCommunicator, TelegramHostCommunicator>();
                return;
            }

            services.AddSingleton<IHostCommunicator, SimulationHostCommunicator>();
            services.AddSingleton(_ => simulateOptions);
            services.AddHostedService<PollingBackgroundService<LongLoopSimulator>>();
            services.AddHostedService<PollingBackgroundService<ShortLoopSimulator>>();
            services.AddHostedService<PollingBackgroundService<InputLoopSimulator>>();

            services.AddTransient<LongLoopSimulator>();
            services.AddTransient<ShortLoopSimulator>();
            services.AddTransient<InputLoopSimulator>();
        });
        return hostBuilder;
    }
}