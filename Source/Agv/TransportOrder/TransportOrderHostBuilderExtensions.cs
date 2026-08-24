using FluentValidation;
using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.MassTransit;
using Gebhardt.Acc.Common.Memory;
using Gebhardt.Acc.Common.Notification;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Common.Services.Significance;
using Gebhardt.Acc.TransportOrder.Core.CmdOptions;
using Gebhardt.Acc.TransportOrder.Core.Configurations;
using Gebhardt.Acc.TransportOrder.Core.Services;
using Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;
using Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices.Housekeeping;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler.ActionGroupHandlers;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Domain.Services.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Domain.Services.Station;
using Gebhardt.Acc.TransportOrder.Domain.Services.TrafficManagement;
using Gebhardt.Acc.TransportOrder.Domain.Validators;
using Gebhardt.Acc.TransportOrder.Messages;
using Gebhardt.Acc.TransportOrder.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Gebhardt.Acc.TransportOrder.Routing.Base;
using Gebhardt.Acc.TransportOrder.Routing.Implementations;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Configurations;
using Gebhardt.Core.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Cargo = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.Cargo;

namespace Gebhardt.Acc.TransportOrder;

/// <summary>
/// IHostBuilder extensions
/// </summary>
public static class TransportOrderHostBuilderExtensions
{
    /// <summary>
    /// Configures the HostBuilder used by the TransportOrderCore <paramref name="hostBuilder" />
    /// </summary>
    public static IHostBuilder AddTransportOrderCore(this IHostBuilder hostBuilder, SystemOptions systemOptions)
    {
        hostBuilder.ConfigureServices((context, services) =>
        {
            RegisterConfigurationServices(systemOptions, services, context);

            RegisterTrafficManagementServices(services);

            RegisterMapServices(services);

            RegisterAgvServices(services, context.Configuration);

            RegisterRepositoryServices(services);

            RegisterValidators(services);

            RegisterSignificanceCheckers(services);

            RegisterMiscellaneousServices(services);

            services.ConfigureAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            services.AddNotifications(new NotificationManagerConfiguration(TransportOrderNotifications.NotificationTypes, MaximumConnectionRetries: 10));
            services.AddSingleton<ITransportOrderNotificationSender, TransportOrderNotificationSender>();
        });

        return hostBuilder;
    }

    private static void RegisterConfigurationServices(SystemOptions systemOptions, IServiceCollection services, HostBuilderContext context)
    {
        services.AddSingleton(_ => systemOptions);
        services.AddSingleton(TimeProvider.System);
        services.AddHostedService<AgvSettingsInitializer>();
        services.AddHostedService<InMemoryCacheInitializer>();
        services.AddHostedService<AgvPositionPersistenceService>();
        services.AddHostedService<StationOnlineSyncService>();
        services.AddHostedService<FactoryTimeZoneInitializer>();
        services.AddHostedService<MapInitializer>();
        services.AddHostedService<Housekeeping>();
        services.AddSingleton<IConcurrentDictionaryProvider<Guid, AgvSettingsReadModel>, InMemoryAgvSettingsProvider>();
        services.AddTransient<IAgvSettingsProvider, EfAgvSettingsProvider>();
        // -  database
        services.AddDbContextFactory<TransportOrderDbContext>((sp, builder) =>
        {
            string? connectionString = context.Configuration.GetConnectionString(DbProviders.SqlServer)
                                       ?? context.Configuration.GetConnectionString(TransportOrderDbContext.ConfigSectionName);

            builder.UseSqlServer(connectionString, m =>
            {
                m.MigrationsHistoryTable(IDbContext<TransportOrderDbContext>.MigrationTableName);
                m.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
            builder.AddInterceptors(sp.GetRequiredService<SqlPrintInterceptor>(), new EfCommandMetricsInterceptor());
        });

        services.AddHealthChecks()
            .AddDbContextCheck<TransportOrderDbContext>();

        services.AddSingleton<SqlPrintInterceptor>();
    }

    private static void RegisterMapServices(IServiceCollection services)
    {
        services.AddTransient<IChargingPointManager, ChargingPointManager>();
        services.AddTransient<IParkPointService, ParkPointService>();
        services.AddTransient<IQueueManager, QueueManager>();
        services.AddTransient<IRouteService, RouteService>();
        services.AddTransient<IAgvRouteRetrievalService, AgvRouteRetrievalService>();
        services.AddSingleton<IRouteFinder, AStarRouteFinder>();
        services.AddTransient<IRoutingPlausibilityChecker, NoCheckPlausibilityChecker>();
    }

    private static void RegisterAgvServices(IServiceCollection services, IConfiguration configuration)
    {
        // Actions
        string selectionMode = OrderSelectionMode.GetModeFromConfiguration(configuration);
        Console.WriteLine($"Using order selection mode {selectionMode}");
        switch (selectionMode)
        {
            case OrderSelectionMode.FastestRoute:
                services.AddTransient<IChooseAgvActionGroupService, ShortestRouteChooser>();
                break;

            case OrderSelectionMode.OldestOrder:
                services.AddTransient<IChooseAgvActionGroupService, OldestActionGroupChooser>();
                break;

            case OrderSelectionMode.Alternating:
                services.AddSingleton<IChooseAgvActionGroupService, AlternatingModeChooser>();
                break;

            case OrderSelectionMode.FastestOfXOldest:
                services.AddTransient<IChooseAgvActionGroupService, FastestRouteOfXOldestActionGroupsChooser>();
                break;

            case OrderSelectionMode.Threshold:
                services.AddTransient<IChooseAgvActionGroupService, FastestRouteAndThresholdChooser>();
                break;

            case OrderSelectionMode.Dynamic:
                services.AddTransient<IChooseAgvActionGroupService, DynamicPrioritizationChooser>();
                break;

            default:
                services.AddTransient<IChooseAgvActionGroupService, ShortestRouteChooser>();
                break;
        }

        // Agv behaviour
        services.AddTransient<IMaintenanceService, MaintenanceService>();
    }

    private static void RegisterRepositoryServices(IServiceCollection services)
    {
        services.AddTransient<IActionGroupRetrievalService, ActionGroupRetrievalService>();
        services.AddTransient<IActionGroupRepository, EfActionGroupRepository>();
        services.AddTransient<IAgvRepository, AgvRepository>();
        services.AddTransient<IAgvDataRepository, EfAgvDataRepository>();
        services.AddTransient<IAgvTypeRepository, EfAgvTypeRepository>();
        services.AddTransient<IStationRepository, EfStationRepository>();
        services.AddTransient<IZoneRepository, EfZoneRepository>();
        services.AddTransient<IQueueRepository, EfQueueRepository>();
        services.AddTransient<ILogEntryRepository, EfLogEntryRepository>();
        services.AddTransient<ILoggingService, LoggingService>();
        services.AddTransient<IDecisionPointActionRepository, EfDecisionPointActionRepository>();
        services.AddTransient<IWaitingPointActionRepository, EfWaitingPointActionRepository>();

        services.AddTransient<IMapRepository, EfMapRepository>();
        services.AddTransient<IBackgroundEditorRepository, EfBackgroundEditorRepository>();

        services.AddTransient<IGraphRepository, EfGraphRepository>();
        services.AddTransient<INodeRepository, GraphNodeRepository>();
        services.AddTransient<IMaintenanceLogRepository, EfMaintenanceLogRepository>();

        services.AddSingleton<IConcurrentDictionaryProvider<Guid, AgvPositionStatus>, AgvPositionDictionaryProvider>();
        services.AddSingleton<IConcurrentDictionaryProvider<Guid, Lazy<List<AgvError>>>, AgvErrorDictionaryProvider>();
        services.AddSingleton<IConcurrentDictionaryProvider<Guid, Location>, TriggerLocationDictionaryProvider>();
        services.AddSingleton<IConcurrentDictionaryProvider<Guid, List<Cargo>>, AgvCargoDictionaryProvider>();
        services.AddSingleton<IAgvRoutePlanCacheProvider, AgvRoutePlanCache>();
        services.AddSingleton<StationOnlineDictionaryProvider>();
        services.AddSingleton<IStationOnlineRepository, StationOnlineRepository>();
        services.AddTransient<IAgvPositionRepository, AgvPositionRepository>();
        services.AddTransient<ITriggerRepository, EfTriggerRepository>();
        services.AddTransient<IRestrictedAreaRepository, EfRestrictedAreaRepository>();
        services.AddTransient<IAgvErrorRepository, EfAgvErrorRepository>();
        services.AddTransient<ICargoRepository, CargoRepository>();
        services.AddTransient<IFactoryTimeRepository, FactoryTimeRepository>();
        services.AddTransient<IAgvDb, AgvDb>();
        services.AddTransient<IAgvInstantActionDefinitionRepository, EfAgvInstantActionDefinitionRepository>();
        services.AddTransient<IOwnedFilterRepository, OwnedFilterRepository>();
        services.AddTransient<IHousekeepingConfigurationRepository, EfHousekeepingConfigurationRepository>();
        services.AddTransient<IHousekeepingAction, ErrorHousekeepingAction>();
        services.AddTransient<IHousekeepingAction, WifiHousekeepingAction>();
        services.AddTransient<IHousekeepingAction, LogHousekeepingAction>();
        services.AddTransient<IHousekeepingAction, ActionGroupHousekeepingAction>();
        services.AddTransient<HousekeepingJob>();
        services.AddTransient<IOwnedFilterTemplateRepository, OwnedFilterTemplateRepository>();
        services.AddSingleton<IEdgePolygonProvider, DirectEdgePolygonProvider>();
        services.AddSingleton<IEdgePolygonProvider, BezierEdgePolygonProvider>();
        services.AddSingleton<IMarginStrategy>(new SafetyMarginStrategy(0.2));
        services.AddTransient<IBoundingBoxRepository, EfBoundingBoxRepository>();
        services.AddTransient<INewGraphRepository, NewGraphRepository>();
    }

    private static void RegisterValidators(IServiceCollection services)
    {
        services.AddTransient<IActionGroupValidationService, ActionGroupValidationService>();
        services.AddTransient<IValidator<ActionGroup>, ActionGroupValidator>();
        services.AddTransient<IValidator<AgvAction>, AgvActionValidator>();
    }

    private static void RegisterSignificanceCheckers(IServiceCollection services)
    {
        services.AddSingleton(new AgvPositionConfiguration(0.005, Mathematics.ConvertToRadians(0.1), 0.001));
        services.AddSingleton<ISignificanceChecker<AgvPosition>, AgvPositionSignificanceChecker>();
    }

    private static void RegisterTrafficManagementServices(IServiceCollection services)
    {
        services.AddScoped<ITrafficManager, TrafficManager>();

        services.AddTrafficManagementServices();
    }

    private static void RegisterMiscellaneousServices(IServiceCollection services)
    {
        services.AddSingleton<PriorityAwareTargetSelector>();
        services.AddSingleton<PriorityAwareWaitingHandler>();
        services.AddSingleton<UnifiedTargetSelector>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<UnifiedTargetSelector>());
        services.AddSingleton<ITargetSelector>(sp => sp.GetRequiredService<UnifiedTargetSelector>());
        services.AddSingleton<IWaitingHandler>(sp => sp.GetRequiredService<UnifiedTargetSelector>());
        services.AddTransient(typeof(IMessageReceiver<>), typeof(MessageReceiver<>));
        services.AddTransient(typeof(IMessageAwaiter<>), typeof(MessageAwaiter<>));
        services.AddSingleton(typeof(ISemaphoreProvider<>), typeof(SemaphoreProvider<>));
        services.AddSingleton(typeof(ICache<,>), typeof(Cache<,>));

        services.AddSingleton<IConcurrentDictionaryProvider<string, (double value, DateTime lastUpdated)>, DeviationCacheProvider>();
        services.AddTransient<IActionGroupDurationEstimationService, ActionGroupDurationEstimationService>();
        services.AddTransient<IMetricCollectionService, PrometheusMetricCollectionService>();
        services.AddTransient<ActionCommandHelper>();
        services.AddTransient<IStationAgvLimitGuard, StationAgvLimitGuard>();
    }
}