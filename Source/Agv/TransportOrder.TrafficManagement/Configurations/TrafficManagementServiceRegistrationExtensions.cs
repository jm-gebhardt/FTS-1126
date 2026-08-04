using Gebhardt.Acc.TransportOrder.TrafficManagement.EventHandler;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Reservations;
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
        services.AddSingleton<ReservationManager>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<TrafficManagementTickState>();
        services.AddHostedService<TrafficManagementTickPublisher>();
    }
}