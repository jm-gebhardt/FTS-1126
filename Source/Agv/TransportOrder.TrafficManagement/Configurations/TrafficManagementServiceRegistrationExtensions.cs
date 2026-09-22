using Gebhardt.Acc.TransportOrder.TrafficManagement.Agvs;
using Gebhardt.Acc.TransportOrder.TrafficManagement.EventHandler;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Graph;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Reservations;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Routes.Generation;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Routes.Manager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Configurations;

/// <summary>
/// Extension methods for registering services related to traffic management in the dependency injection container.
/// </summary>
public static class TrafficManagementServiceRegistrationExtensions
{
    /// <summary>
    /// Adds all services for this project to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection</param>
    public static void AddTrafficManagementServices(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<ReservationManager>();
        services.AddSingleton<RouteGenerator>();
        services.AddSingleton<RouteManager>();
        services.AddSingleton<GraphManager>();
        services.AddSingleton<AgvProvider>();

        services.AddSingleton<TrafficManagementTickState>();
        services.AddHostedService<TrafficManagementTickPublisher>();
    }

    /// <summary>
    /// Adds the hosted service that loads the graph on startup.
    /// </summary>
    public static void AddTrafficManagementGraphInitializer(this IServiceCollection services)
    {
        services.AddHostedService<GraphInitializer>();
    }
}