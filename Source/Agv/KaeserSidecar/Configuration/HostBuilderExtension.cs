using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Services;
using Gebhardt.Acc.KaeserSidecar.CmdOptions;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;
using Gebhardt.Acc.KaeserSidecar.Services.BackgroundServices;
using Gebhardt.Acc.KaeserSidecar.SimulationMode;
using Gebhardt.Acc.PrismatCommunication.Configuration;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Services;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;
using Gebhardt.Acc.TransportOrder.Domain.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using InitializationProvider = Gebhardt.Acc.KaeserSidecar.Services.BackgroundServices.InitializationProvider;
using IStationRepository = Gebhardt.Acc.KaeserSidecar.Domain.Repositories.IStationRepository;

namespace Gebhardt.Acc.KaeserSidecar.Configuration;

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
                services.AddSingleton(TimeProvider.System);
                //Configure masstransit
                services.ConfigureServicesMassTransit(context.Configuration, x => { x.AddBusConsumers(); });
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
    public static IHostBuilder AddKaeserSidecar(this IHostBuilder hostBuilder, SimulateOptions? simulateOptions)
    {
        ConfigurationTypeManager.Instance.InitializeConfigurationTypes();
        hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddConfigurationTypeManager();
            services.AddSingleton(typeof(ISemaphoreProvider<>), typeof(SemaphoreProvider<>));
            services.AddSingleton<IWaitingHandler, WaitingHandler>();
            services.AddSingleton<IHostedService>(sp => (IHostedService) sp.GetRequiredService<IWaitingHandler>());

            services.AddSingleton<ITargetSelector, TargetSelector>();
            services.AddSingleton<IHostedService>(sp => (IHostedService) sp.GetRequiredService<ITargetSelector>());

            services.AddHostedService<InitializationProvider>();
            // Add prismat communication db connection
            services.AddDbContext<PrismatCommunicationDbContext>(o =>
                o.UseSqlServer(context.Configuration.GetConnectionString(PrismatCommunicationDbContext.ConfigSectionName)));
            services.AddTransient<IPrismatCommunicationShortLoopWeTargetStationRepository, PrismatCommunicationShortLoopWeTargetStationRepository>();
            services.AddTransient<IPrismatCommunicationHostOrderRepository, PrismatCommunicationHostOrderRepository>();
            services.AddTransient<IHostOrderRepository>(sp => sp.GetRequiredService<IPrismatCommunicationHostOrderRepository>());
            services.AddTransient<IPrismatCommunicationStationRepository, PrismatCommunicationStationRepository>();
            services.AddTransient<PrismatCommunication.Domain.Repositories.IStationRepository>(sp => sp.GetRequiredService<IPrismatCommunicationStationRepository>());
            services.AddTransient<IPrismatCommunicationSystemStatusRepository, PrismatCommunicationSystemStatusRepository>();
            services.AddTransient<ISystemStatusRepository>(sp => sp.GetRequiredService<IPrismatCommunicationSystemStatusRepository>());

            services.AddTransient<IAklAgvLimitGuard, AklAgvLimitGuard>();
            services.AddTransient<ITransportOrderDb, TransportOrderDb>();
            services.AddTransient<ITransportOrderActionGroupRepository, TransportOrderActionGroupRepository>();
            services.AddTransient<ITransportOrderAgvRepository, TransportOrderAgvRepository>();
            services.AddTransient<IJamStateRepository, EfJamStateRepository>();
            services.AddTransient<IDecisionPointActionRepository, EfDecisionPointActionRepository>();
            services.AddTransient<IWaitingPointActionRepository, EfWaitingPointActionRepository>();
            services.AddTransient<ITransportOrderStationRepository, TransportOrderStationRepository>();
            services.AddTransient<ITransportOrderAgvSettingsProvider, TransportOrderAgvSettingsProvider>();
            services.AddSingleton<IConcurrentDictionaryProvider<Guid, AgvSettingsReadModel>, InMemoryAgvSettingsProvider>();
            // Add transport order db connection
            services.AddDbContextFactory<TransportOrderDbContext>(builder =>
            {
                string? connectionString = context.Configuration.GetConnectionString(TransportOrderDbContext.ConfigSectionName);

                builder.UseSqlServer(connectionString);
            });

            services.AddDbContextFactory<KaeserSidecarDbContext>(builder =>
            {
                string? connectionString = context.Configuration.GetConnectionString(KaeserSidecarDbContext.ConfigSectionName);
                builder.UseSqlServer(connectionString, m =>
                {
                    m.MigrationsHistoryTable(IDbContext<KaeserSidecarDbContext>.MigrationTableName);
                    m.UseCompatibilityLevel(120); // EF9 compatibility level
                });
            });
            services.AddDbContextFactory<PrismatCommunicationDbContext>(o =>
                o.UseSqlServer(context.Configuration.GetConnectionString(PrismatCommunicationDbContext.ConfigSectionName)));
            
            services.AddTransient<IAgvSettingsService, AgvSettingsService>();
            services.AddSingleton<IStationRepository, EfStationRepository>();
            services.AddSingleton<IOrderRepository, EfOrderRepository>();
            services.TryAddKaeserGraphConfiguration(simulateOptions != null);
            if (simulateOptions != null)
            {
                services.AddSingleton<IConveyorCommunicator, SimulationConveyorCommunicator>();
            }
            else
            {
                services.AddConveyorApi(context.Configuration);
                services.AddSingleton<IConveyorCommunicator, TelegramConveyorCommunicator>();
            }
        });
        return hostBuilder;
    }
}