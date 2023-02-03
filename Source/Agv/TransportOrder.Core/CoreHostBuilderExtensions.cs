using AutoMapper;
using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.Common.Services.Detectors;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence;
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
            hostBuilder.ConfigureServices((context, services) =>
            {
                // Transients
                services.AddTransient<IAgvRegistry, EfAgvRegistry>();
                services.AddTransient<IOrderRegistry, EfOrderRegistry>();
                services.AddTransient<IMapRegistry, EfMapRegistry>();
                services.AddTransient<IOrderAuditRegistry, EfOrderAuditRegistry>();
                services.AddTransient<IOrderHubContext>(provider =>
                {
                    var hubContext = provider.GetRequiredService<IHubContext<OrderHub>>();
                    var mapper = provider.GetRequiredService<IMapper>();

                    return new OrderHubContext(hubContext, mapper);
                });
                services.AddTransient<IAgvHubContext>(provider =>
                {
                    var hubContext = provider.GetRequiredService<IHubContext<AgvHub>>();

                    return new AgvHubContext(hubContext);
                });
                services.AddTransient<IEventObserver<OrderSaga>, OrderEventObserver>();

                // Singletons
                services.AddSingleton<IStateObserver<OrderSaga>, OrderStateObserver>();
                services.AddSingleton<IStateObserver<AgvSaga>, AgvStateObserver>();
                services.AddSingleton<IAgvPositionRegistry, AgvPositionRegistry>();
                services.AddSingleton<IChangeDetector<AgvPositionStatus>, ChangeDetector<AgvPositionStatus>>();
                services.AddSingleton<IRoutingPlausibilityChecker, NoCheckPlausibilityChecker>();
                services.AddSingleton(_ => productionOptions);

                services.AddSingleton<MassTransitBusEventConsumer>();
                services.AddSingleton<ActionManager>();
                
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
                    ConfigureServicesMassTransit.ConfigureServices(services, context.Configuration, x =>
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
                        x.AddConsumer<GetMapImageRequestHandler>();
                        x.AddConsumer<GetMapListRequestHandler>();
                        
                        x.AddRequestClient<ReserveAgv>();
                        x.AddRequestClient<AssignAgv>();
                        x.AddRequestClient<GetMapImageRequest>();
                        x.AddRequestClient<GetMapListRequest>();
                        
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
