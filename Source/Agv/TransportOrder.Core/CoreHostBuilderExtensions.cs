using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Configurations;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.Common.Services.Detectors;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Db;
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
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Handlers.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Requests.Queries;
using Gebhardt.StoreWare.Agv.Common.Persistence;

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
                // Transients
                services.AddTransient<IOrderHubContext>(provider =>
                {
                    var hubContext = provider.GetRequiredService<IHubContext<OrderHub>>();
                    var mapper = provider.GetRequiredService<IMapper>();

                    return new OrderHubContext(hubContext, mapper);
                });
                services.AddTransient<IAgvHubContext>(provider =>
                {
                    var hubContext = provider.GetRequiredService<IHubContext<AgvHub>>();
                    var mapper = provider.GetRequiredService<IMapper>();

                    return new AgvHubContext(hubContext, mapper);
                });
                services.AddTransient<IEventObserver<OrderSaga>, OrderEventObserver>();
                services.AddTransient<IAgvSettingsProvider, AgvSettingsProvider>();
                services.AddConfigurationTypeManager();
                services.AddSettingsProviders(ConfigurationTypeManager.Instance);

                
                // Scoped
                services.AddScoped<DatabaseConfigurationSeeder>();

                // Singletons
                services.AddSingleton<IStateObserver<OrderSaga>, OrderStateObserver>();
                services.AddSingleton<IStateObserver<AgvSaga>, AgvStateObserver>();
                services.AddSingleton<IChangeDetector<AgvPositionStatus>, ChangeDetector<AgvPositionStatus>>();
                services.AddSingleton<IRoutingPlausibilityChecker, NoCheckPlausibilityChecker>();
                services.AddSingleton(_ => productionOptions);
                services.AddSingleton<MassTransitBusEventConsumer>();
                services.AddSingleton<ActionManager>();
                services.AddSingleton<ISagaSettingsProvider<AgvSettings, AgvSaga>, SagaSettingsProvider<AgvSettings, AgvSaga, SagaPersistenceDbContext>>();
                services.AddSingleton<IAgvSettingsProvider, AgvSettingsProvider>();

                // Registries
                services.AddSingleton<IAgvActionRegistry, EfAgvActionRegistry>();
                services.AddSingleton<IAgvRegistry, EfAgvRegistry>();
                services.AddSingleton<IOrderRegistry, EfOrderRegistry>();
                services.AddSingleton<IOrderAuditRegistry, EfOrderAuditRegistry>();
                services.AddSingleton<IMapRegistry, EfMapRegistry>();
                services.AddSingleton<IGraphRegistry, EfGraphRegistry>();
                services.AddSingleton<IAgvPositionRegistry, AgvPositionRegistry>();

                services.AddConfigurationTypeManager();
                services.AddSettingsProviders(ConfigurationTypeManager.Instance);

                services.AddHostedService<ActionManagerBackgroundService>();

                services.AddDbContextFactory<TransportOrderDbContext>(builder =>
                {
                    builder.UseSqlServer(context.Configuration.GetConnectionString(TransportOrderDbContext.ConfigSectionName));
                });
                services.AddDbContextFactory<SagaPersistenceDbContext>(builder =>
                {
                    builder.UseSqlServer(context.Configuration.GetConnectionString(SagaPersistenceDbContext.ConfigSectionName), m =>
                    {
                        m.MigrationsHistoryTable(SagaPersistenceDbContext.MigrationTableName);
                    });
                });

                // Automapper
                services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

                try
                {
                    services.ConfigureServicesMassTransit(context.Configuration, x =>
                    {
                        x.AddSagaStateMachine<OrderStateMachine, OrderSaga>()
                            .AddPersistence<OrderSaga, SagaPersistenceDbContext>();
                        x.AddSagaStateMachine<AgvStateMachine, AgvSaga>()
                            .AddPersistence<AgvSaga, SagaPersistenceDbContext>();

                        x.AddConsumer<AssignAgvConsumer>();
                        x.AddConsumer<MassTransitBusEventConsumer>();
                        x.AddConsumer<ActionManager>();
                        x.AddConsumer<AgvChargingService>();
                        x.AddConsumer<TransportOrderChangedConsumer>();
                        x.AddConsumer<AgvPositionReceivedConsumer>();
                        x.AddConsumer<AgvPositionChangedConsumer>();
                        x.AddConsumer<AgvPropertyChangedConsumer>();
                        x.AddConsumer<AgvInitializer>();

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
                        x.AddConsumer<SetIdleCommandHandler>();

                        // orders
                        x.AddConsumer<CompleteOrderCommandHandler>();
                        x.AddConsumer<GetOrdersRequestHandler>();
                        x.AddConsumer<ResetOrderCommandHandler>();

                        // request clients
                        // ########################################
                        // maps
                        x.AddRequestClient<GetMapImageRequest>();
                        x.AddRequestClient<GetMapListRequest>();

                        // agv
                        x.AddRequestClient<RequestAgvDataCommandHandler>();
                        x.AddRequestClient<RequestAgvGraphCommandHandler>();
                        x.AddRequestClient<GetAgvDataRequestHandler>();
                        x.AddRequestClient<GetAgvGraphRequestHandler>();
                        x.AddRequestClient<GetAgvPositionsRequestHandler>();
                        x.AddRequestClient<GetAgvsRequestHandler>();
                        x.AddRequestClient<GetAgvStatusRequestHandler>();
                        x.AddRequestClient<LockAgvCommandHandler>();
                        x.AddRequestClient<SetIdleCommandHandler>();

                        // orders
                        x.AddConsumer<CompleteOrderCommandHandler>();
                        x.AddConsumer<GetOrdersRequestHandler>();
                        x.AddConsumer<ResetOrderCommandHandler>();

                        x.AddDelayedMessageScheduler();
                    }, rabbitMqConfiguration: (_, cfg) =>
                    {
                        cfg.UseDelayedMessageScheduler();
                        cfg.ConnectBusObserver(new BusObserver());
                    });
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
