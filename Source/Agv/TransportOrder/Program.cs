using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using AutoMapper;
using CommandLine;
using MassTransit;
using Prometheus;
using Gebhardt.StoreWare.Agv.Common;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Services.Detectors;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi;

var initialLogger = Gebhardt.StoreWare.Agv.Common.Logging.InitialLogger.Create<Program>();

try
{
    IHostBuilder? hostBuilder = null;

    Parser.Default.ParseArguments<ProductionOptions>(args)
        .WithParsed(productionOptions =>
        {
            hostBuilder = GebhardtStartup.CreateHostBuilder(args)
                .ConfigureServices((context, services) =>
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
                        var mapper = provider.GetRequiredService<IMapper>();

                        return new AgvHubContext(hubContext, mapper);
                    });
                    services.AddTransient<IEventObserver<OrderSaga>, OrderEventObserver>();

                    // Singletons
                    services.AddSingleton<IStateObserver<OrderSaga>, OrderStateObserver>();
                    services.AddSingleton<IStateObserver<AgvSaga>, AgvStateObserver>();
                    services.AddSingleton<IAgvPositionRegistry, AgvPositionRegistry>();
                    services.AddSingleton<IChangeDetector<AgvPositionStatus>, ChangeDetector<AgvPositionStatus>>();
                    services.AddSingleton<IRoutingPlausibilityChecker, RoutingPlausibilityChecker>();
                    services.AddSingleton(_ => productionOptions);

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
                            x.AddConsumer<TransportOrderChangedConsumer>();
                            x.AddConsumer<AgvPositionReceivedConsumer>();
                            x.AddConsumer<AgvPositionChangedConsumer>();
                            x.AddConsumer<AgvInitializer>();

                            x.AddRequestClient<ReserveAgv>();

                            x.AddDelayedMessageScheduler();
                        }, rabbitMqConfiguration: (_, cfg) =>
                        {
                            cfg.UseDelayedMessageScheduler();
                            cfg.ConnectBusObserver(new Gebhardt.StoreWare.Agv.Common.Services.BusObserver());
                        });
                    }
                    catch (InvalidOperationException e)
                    {
                        initialLogger.LogError(e, "MassTransit could not be started");
                    }
                });
        });

    // By using the generic overload of WithParsed above hostBuilder will be null for invalid command line arguments.
    if (hostBuilder != null)
    {
        hostBuilder.ConfigureWebHost(host =>
        {
            host.UseKestrel()
                .AddTransportOrderWebApi()
                .Configure(app =>
                {
                    app.UseMetricServer();
                    app.UseTransportOrderWebApi();
                });
        });

        var host = hostBuilder.Build();

        using (var scope = host.Services.CreateScope())
        {
            // Apply migrations if SagaPersistenceDbContext is available
            var db = scope.ServiceProvider.GetService<SagaPersistenceDbContext>();
            db?.Database.Migrate();
        }

        host.Run();
        return 0;
    }
    else
    {
        initialLogger.LogWarning("Invalid command line argument(s)");
        return 1;
    }
}
catch (Exception ex)
{
    initialLogger.LogError(ex, "Unhandled exception");
    return 1;
}
