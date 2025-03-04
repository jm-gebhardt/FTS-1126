using FluentValidation;
using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.MassTransit;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Common.Services.Significance;
using Gebhardt.Acc.Common.Time;
using Gebhardt.Acc.TransportOrder.Core.CmdOptions;
using Gebhardt.Acc.TransportOrder.Core.Configurations;
using Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Domain.Validators;
using Gebhardt.Acc.TransportOrder.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Gebhardt.Acc.TransportOrder.Routing.Base;
using Gebhardt.Acc.TransportOrder.Routing.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder;

/// <summary>
/// IHostBuilder extensions
/// </summary>
public static class TransportOrderHostBuilderExtensions
{
    /// <summary>
    /// Configures the HostBuilder used by the TransportOrderCore <paramref name="hostBuilder" />
    /// </summary>
    public static IHostBuilder AddTransportOrderCore(this IHostBuilder hostBuilder, ProductionOptions productionOptions)
    {
        hostBuilder.ConfigureServices((context, services) =>
        {
            RegisterConfigurationServices(productionOptions, services, context);

            RegisterMapServices(services);

            RegisterAgvServices(services, context.Configuration);

            RegisterRepositoryServices(services);

            RegisterValidators(services);

            RegisterSignificanceCheckers(services);

            RegisterMiscellaneousServices(services);

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        });

        return hostBuilder;
    }

    private static void RegisterConfigurationServices(ProductionOptions productionOptions, IServiceCollection services, HostBuilderContext context)
    {
        // Configuration
        services.AddSingleton(_ => productionOptions);
       
        services.AddSingleton<ITimeProvider, DateTimeNowUtcProvider>();
        services.AddHostedService<AgvSettingsInitializer>();
        services.AddHostedService<InMemoryCacheInitializer>();
        services.AddHostedService<FactoryTimeZoneInitializer>();
        services.AddSingleton<IConcurrentDictionaryProvider<Guid, AgvSettingsReadModel>, InMemoryAgvSettingsProvider>();
        services.AddTransient<IAgvSettingsProvider, EfAgvSettingsProvider>();
        // -  database
        services.AddDbContextFactory<TransportOrderDbContext>(builder =>
        {
            string? connectionString = context.Configuration.GetConnectionString(TransportOrderDbContext.ConfigSectionName);

            builder.UseSqlServer(connectionString, m =>
            {
                m.MigrationsHistoryTable(TransportOrderDbContext.MigrationTableName);
                m.UseCompatibilityLevel(120); // avoid using OPENJSON in translated sql. In EF9 : use m.TranslateParameterizedCollectionsToConstants() instead
            }).LogTo(Console.WriteLine, LogLevel.Information);
        });
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
        services.AddTransient<IStationRepository, EfStationRepository>();
        services.AddTransient<IQueueRepository, EfQueueRepository>();

        services.AddTransient<IMapRepository, EfMapRepository>();

        services.AddTransient<IGraphRepository, EfGraphRepository>();
        services.AddTransient<INodeRepository, EfNodeRepository>();

        services.AddTransient<INodeStateRepository, EfNodeStateRepository>();

        services.AddSingleton<IConcurrentDictionaryProvider<Guid, AgvPositionStatus>, AgvPositionDictionaryProvider>();
        services.AddSingleton<IConcurrentDictionaryProvider<Guid, Lazy<List<AgvError>>>, AgvErrorDictionaryProvider>();
        services.AddSingleton<IConcurrentDictionaryProvider<Guid, Location>, TriggerLocationDictionaryProvider>();
        services.AddSingleton<IConcurrentDictionaryProvider<Guid, List<Domain.Models.Agv.Cargo>>, AgvCargoDictionaryProvider>();
        services.AddTransient<IAgvPositionRepository, AgvPositionRepository>();
        services.AddTransient<ITriggerRepository, EfTriggerRepository>();
        services.AddTransient<IRestrictedAreaRepository, EfRestrictedAreaRepository>();
        services.AddTransient<IAgvErrorRepository, EfAgvErrorRepository>();
        services.AddTransient<ICargoRepository, CargoRepository>();
        services.AddTransient<IFactoryTimeRepository, FactoryTimeRepository>();
        services.AddTransient<IAgvDb, AgvDb>();
        services.AddTransient<IAgvInstantActionDefinitionRepository, EfAgvInstantActionDefinitionRepository>();
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

    private static void RegisterMiscellaneousServices(IServiceCollection services)
    {
        services.AddTransient(typeof(IMessageReceiver<>), typeof(MessageReceiver<>));
        services.AddTransient(typeof(IMessageAwaiter<>), typeof(MessageAwaiter<>));
        services.AddSingleton(typeof(ISemaphoreProvider<>), typeof(SemaphoreProvider<>));

        services.AddSingleton<IConcurrentDictionaryProvider<string, (double value, DateTime lastUpdated)>, DeviationCacheProvider>();
        services.AddTransient<IActionGroupDurationEstimationService, ActionGroupDurationEstimationService>();
        services.AddTransient<IMetricCollectionService, PrometheusMetricCollectionService>();
        services.AddTransient<ActionCommandHelper>();
    }
}