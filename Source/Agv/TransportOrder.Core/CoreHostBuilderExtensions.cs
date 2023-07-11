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

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core
{
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
                // Scoped
                services.AddScoped<DatabaseConfigurationSeeder<TransportOrderDbContext>>();

                // Singletons
                services.AddSingleton<IStateObserver<AgvSaga>, AgvStateObserver>();
                services.AddSingleton<IChangeDetector<AgvPositionStatus>, ChangeDetector<AgvPositionStatus>>();
                services.AddSingleton<IRoutingPlausibilityChecker, NoCheckPlausibilityChecker>();
                services.AddSingleton(_ => productionOptions);
                services.AddSingleton<IActionManager, ActionManager>();
                services.AddSingleton<IChooseAgvActionGroupService, ShortestRouteChooser>();
                services.AddSingleton<IAgvService, AgvService>();
                services.AddSingleton<ISettingsProvider<AgvSettings>, SettingsProvider<AgvSettings>>();
                services.AddSingleton<IRouteService, RouteService>();
                services.AddSingleton<IChargingService, BaseAgvChargingService>();
                services.AddSingleton<IMaintenanceService, MaintenanceService>();
                services.AddSingleton<IAgvLoadCapabilityService, AgvLoadCapabilityService>();
                services.AddSingleton<IStartActionService, StartActionService>();
                services.AddSingleton<IChargingPointManager, ChargingPointManager>();
                services.AddSingleton<IParkPointService, ParkPointService>();

                services.AddSingleton<IAgvStateService, AgvStateService>();

                // Registries
                services.AddSingleton<IActionGroupRegistry, EfActionGroupRegistry>();
                services.AddSingleton<IAgvRegistry, EfAgvRegistry>();
                services.AddSingleton<IAgvDataRegistry, EfAgvDataRegistry>();
                services.AddSingleton<IMapRegistry, EfMapRegistry>();
                services.AddSingleton<IGraphRegistry, EfGraphRegistry>();
                services.AddSingleton<INodeStateRegistry, EfNodeStateRegistry>();
                services.AddSingleton<IAgvPositionRegistry, AgvPositionRegistry>();
                services.AddSingleton<IAgvErrorRegistry, EfAgvErrorRegistry>();

                services.AddConfigurationTypeManager();
                services.AddSettingsProviders(ConfigurationTypeManager.Instance);

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

                // Automapper
                services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            });

            return hostBuilder;
        }
    }
}