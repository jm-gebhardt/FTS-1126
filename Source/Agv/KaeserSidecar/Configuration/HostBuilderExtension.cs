using Gebhardt.Acc.Common.Configurations;
using Gebhardt.Acc.Common.Configurations.Extensions;
using Gebhardt.Acc.Common.Time;
using Gebhardt.Acc.KaeserConveyorTelegramApi.Services;
using Gebhardt.Acc.KaeserSidecar.CmdOptions;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;
using Gebhardt.Acc.KaeserSidecar.SimulationMode;
using Gebhardt.Acc.PrismatCommunication.Configuration;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                services.AddSingleton<ITimeProvider, DateTimeNowUtcProvider>();
                services.ConfigureServicesMassTransit(context.Configuration, x =>
                {
                    x.AddBusConsumers();
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
    public static IHostBuilder AddKaeserSidecar(this IHostBuilder hostBuilder, SimulateOptions? simulateOptions)
    {
        ConfigurationTypeManager.Instance.InitializeConfigurationTypes();
        hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddConfigurationTypeManager();

            // Add prismat communication db connection
            services.AddDbContext<PrismatCommunicationDbContext>(o =>
                o.UseSqlServer(context.Configuration.GetConnectionString(PrismatCommunicationDbContext.ConfigSectionName)));
            services.AddTransient<IPrismatCommunicationHostOrderRepository, PrismatCommunicationHostOrderRepository>();
            services.AddTransient<ITransportOrderDb, TransportOrderDb>();
            services.AddTransient<ITransportOrderActionGroupRepository, TransportOrderActionGroupRepository>();
            services.AddTransient<ITransportOrderAgvRepository, TransportOrderAgvRepository>();
            // Add transport order db connection
            services.AddDbContextFactory<TransportOrderDbContext>(builder =>
            {
                string? connectionString = context.Configuration.GetConnectionString(TransportOrderDbContext.ConfigSectionName);

                builder.UseSqlServer(connectionString, m => { m.MigrationsHistoryTable(TransportOrderDbContext.MigrationTableName); });
            });
            services.AddSingleton<IStationRepository, StationRepository>();
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