using Gebhardt.StoreWare.Agv.Common.Configurations;
using Gebhardt.StoreWare.Agv.Common.Services.Detectors;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Gebhardt.StoreWare.Agv.Common.Configurations.Extensions;
using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Saga;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Charging;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Choosers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Maintenance;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Parking;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs.Errors;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Maps;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.ReadModels;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core;

/// <summary>
/// IHostBuilder extensions
/// </summary>
public static class CoreHostBuilderExtensions
{
    /// <summary>
    /// Configured the HostBuilder to used the TransportOrderCore <paramref name="hostBuilder" />
    /// </summary>
    /// <param name="hostBuilder"><see cref="IHostBuilder"/></param>
    /// <param name="productionOptions"><see cref="ProductionOptions"/></param>
    /// <returns><see cref="IHostBuilder"/></returns>
    public static IHostBuilder AddTransportOrderCore(this IHostBuilder hostBuilder, ProductionOptions productionOptions)
    {
        ConfigurationTypeManager.Instance.InitializeConfigurationTypes();

        hostBuilder.ConfigureServices((context, services) =>
        {

            RegisterConfigurationServices(productionOptions, services, context);

            RegisterMapServices(services);

            RegisterAgvServices(services);

            RegisterRegistryServices(services);

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        });

        return hostBuilder;
    }

    private static void RegisterConfigurationServices(ProductionOptions productionOptions, IServiceCollection services, HostBuilderContext context)
    {
        // Configuration
        services.AddConfigurationTypeManager();
        services.AddSettingsProviders(ConfigurationTypeManager.Instance);
        services.AddSingleton(_ => productionOptions);
        services.AddSingleton<ISettingsProvider<AgvSettings>, SettingsProvider<AgvSettings>>();
        // -  database
        services.AddScoped<DatabaseConfigurationSeeder<TransportOrderDbContext>>();
        services.AddDbContextFactory<TransportOrderDbContext>(builder =>
        {
            string? connectionString = context.Configuration.GetConnectionString(TransportOrderDbContext.ConfigSectionName);
            builder.UseSqlServer(connectionString, m => { m.MigrationsHistoryTable(TransportOrderDbContext.MigrationTableName); });
        });
        services.AddDbContextFactory<SagaPersistenceDbContext>(builder =>
        {
            string? connectionString = context.Configuration.GetConnectionString(SagaPersistenceDbContext.ConfigSectionName);
            builder.UseSqlServer(connectionString, m => { m.MigrationsHistoryTable(SagaPersistenceDbContext.MigrationTableName); });
        });
    }

    private static void RegisterMapServices(IServiceCollection services)
    {
        services.AddSingleton<IChargingSemaphoreProvider, ChargingSemaphoreProvider>();
        services.AddTransient<IChargingPointManager, ChargingPointManager>();
        services.AddSingleton<IParkingSemaphoreProvider, ParkingSemaphoreProvider>();
        services.AddTransient<IParkPointService, ParkPointService>();
        services.AddTransient<IRouteService, RouteService>();
        services.AddTransient<IRoutingPlausibilityChecker, NoCheckPlausibilityChecker>();
        services.AddSingleton<IChangeDetector<AgvPositionStatus>, ChangeDetector<AgvPositionStatus>>();
    }

    private static void RegisterAgvServices(IServiceCollection services)
    {
        // Actions
        services.AddTransient<IActionManager, ActionManager>();
        services.AddSingleton<IActionManagerSemaphoreProvider, ActionManagerSemaphoreProvider>();
        services.AddTransient<IStartActionService, StartActionService>();
        services.AddTransient<IChooseAgvActionGroupService, ShortestRouteChooser>();

        // Agv behaviour
        services.AddTransient<IAgvService, AgvService>();
        services.AddTransient<IMaintenanceService, MaintenanceService>();
        services.AddTransient<IAgvStateService, AgvStateService>();
        services.AddTransient<IStateObserver<AgvSaga>, AgvStateObserver>();
        services.AddTransient<IAgvReadyService, AgvReadyService>();
        services.AddTransient<IChargingService, BaseAgvChargingService>();
        services.AddTransient<IAgvLoadCapabilityService, AgvLoadCapabilityService>();
    }

    private static void RegisterRegistryServices(IServiceCollection services)
    {
        services.AddTransient<IActionGroupRetrievalService, ActionGroupRetrievalService>();
        services.AddTransient<IActionGroupRegistry, EfActionGroupRegistry>();
        services.AddTransient<IAgvRegistry, EfAgvRegistry>();
        services.AddTransient<IAgvDataRegistry, EfAgvDataRegistry>();

        services.AddSingleton<IMapRegistrySemaphoreProvider, MapRegistrySemaphoreProvider>();
        services.AddTransient<IMapRegistry, EfMapRegistry>();
        services.AddTransient<IAgvGraphRegistry, EfAgvGraphRegistry>();

        services.AddSingleton<INodeStateDbSemaphoreProvider, NodeStateDbSemaphoreProvider>();
        services.AddTransient<INodeStateRegistry, EfNodeStateRegistry>();

        services.AddSingleton<IConcurrentDictionaryProvider<Guid, AgvPositionStatus>, AgvPositionDictionaryProvider>();
        services.AddTransient<IAgvPositionRegistry, AgvPositionRegistry>();
        services.AddTransient<IAgvErrorRegistry, EfAgvErrorRegistry>();
    }
}