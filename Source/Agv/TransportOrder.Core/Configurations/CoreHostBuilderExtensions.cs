using FluentValidation;
using Gebhardt.StoreWare.Agv.Common.Configurations;
using Gebhardt.StoreWare.Agv.Common.Configurations.Extensions;
using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Services.Detectors;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.SagaStateMachines;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TransportOrder.Domain.Models.ActionGroup;
using TransportOrder.Domain.Models.Agv;
using TransportOrder.Domain.Models.Settings;
using TransportOrder.Domain.Repositories;
using TransportOrder.Domain.SemaphoreProvider;
using TransportOrder.Domain.Services.ActionGroup;
using TransportOrder.Domain.Services.Routing;
using TransportOrder.Domain.Validators;
using TransportOrder.Persistence.Ef.DataAccess;
using TransportOrder.Persistence.Ef.Repositories;
using TransportOrder.Routing;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations;

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

            RegisterValidators(services);

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

            builder.UseSqlServer(connectionString, m =>
            {
                m.MigrationsHistoryTable(TransportOrderDbContext.MigrationTableName);
            });
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
        services.AddTransient<IAgvSagaCommunicationService, AgvSagaCommunicationService>();
        services.AddTransient<IMaintenanceService, MaintenanceService>();
        services.AddTransient<IStateObserver<AgvSaga>, AgvStateObserver>();
        services.AddTransient<IChargingService, BaseAgvChargingService>();
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

    private static void RegisterValidators(IServiceCollection services)
    {
        services.AddTransient<IActionGroupValidationService, ActionGroupValidationService>();
        services.AddTransient<IValidator<ActionGroup>, ActionGroupValidator>();
        services.AddTransient<IValidator<AgvAction>, AgvActionValidator>();
    }
}