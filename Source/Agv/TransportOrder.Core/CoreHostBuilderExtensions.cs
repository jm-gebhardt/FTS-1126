using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Configurations;
using Gebhardt.StoreWare.Agv.Common.Services.Detectors;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Gebhardt.StoreWare.Agv.Common.Configurations.Extensions;
using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Requests.Queries;
using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Saga;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Charging;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Choosers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Maintenance;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Parking;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
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
        /// <param name="initialLogger"><see cref="ILogger"/></param>
        /// <param name="productionOptions"><see cref="ProductionOptions"/></param>
        /// <returns><see cref="IHostBuilder"/></returns>
        public static IHostBuilder AddTransportOrderCore(this IHostBuilder hostBuilder, ILogger initialLogger, ProductionOptions productionOptions)
        {
            ConfigurationTypeManager.Instance.InitializeConfigurationTypes();

            hostBuilder.ConfigureServices((context, services) =>
            {
                services.AddTransient<IAgvHubContext>(provider =>
                {
                    var hubContext = provider.GetRequiredService<IHubContext<AgvHub>>();
                    var mapper = provider.GetRequiredService<IMapper>();

                    return new AgvHubContext(hubContext, mapper);
                });

                services.AddConfigurationTypeManager();
                services.AddSettingsProviders(ConfigurationTypeManager.Instance);

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
                services.AddSingleton<IAgvActionRegistry, EfAgvActionRegistry>();
                services.AddSingleton<IAgvRegistry, EfAgvRegistry>();
                services.AddSingleton<IAgvDataRegistry, EfAgvDataRegistry>();
                services.AddSingleton<IMapRegistry, EfMapRegistry>();
                services.AddSingleton<IGraphRegistry, EfGraphRegistry>();
                services.AddSingleton<INodeStateRegistry, EfNodeStateRegistry>();
                services.AddSingleton<IAgvPositionRegistry, AgvPositionRegistry>();

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

                try
                {
                    services.ConfigureServicesMassTransit(context.Configuration, x =>
                    {
                        x.AddSagaStateMachine<AgvStateMachine, AgvSaga>()
                            .AddPersistence<AgvSaga, SagaPersistenceDbContext>();

                        x.AddConsumer<AgvDataConsumer>();
                        x.AddConsumer<ActionConsumer>();
                        x.AddConsumer<AgvPositionReceivedConsumer>();
                        x.AddConsumer<AgvPositionChangedConsumer>();
                        x.AddConsumer<AgvPropertyChangedConsumer>();
                        x.AddConsumer<AgvBatteryChangedConsumer>();

                        // maps
                        x.AddConsumer<GetMapImageRequestHandler>();
                        x.AddConsumer<GetMapListRequestHandler>();

                        // agv
                        x.AddConsumer<RequestAgvDataCommandHandler>();
                        x.AddConsumer<RequestAgvGraphCommandHandler>();
                        x.AddConsumer<GetAgvDataRequestHandler>();
                        x.AddConsumer<GetAgvGraphRequestHandler>();
                        x.AddConsumer<GetAgvPositionsRequestHandler>();
                        x.AddConsumer<GetAgvsRequestHandler>();
                        x.AddConsumer<GetAgvStatusRequestHandler>();
                        x.AddConsumer<LockAgvCommandHandler>();
                        x.AddConsumer<UnlockAgvCommandHandler>();
                        
                        // Actions
                        x.AddConsumer<GetAgvActionGroupsRequestHandler>();
                        x.AddConsumer<GetAgvActionGroupCountsByStateHandler>();

                        x.AddDelayedMessageScheduler();
                    }, rabbitMqConfiguration: (_, cfg) => { cfg.UseDelayedMessageScheduler(); });
                }
                catch (InvalidOperationException e)
                {
                    initialLogger.LogError(e, "MassTransit could not be started");
                }
            });

            return hostBuilder;
        }
    }
}