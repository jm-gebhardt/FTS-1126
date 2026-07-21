using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.Common.WebApi.Hubs;

/// <summary>
/// Provides registration helpers for SignalR hubs using the abortable lifetime manager.
/// </summary>
public static class HubRegistrationExtensions
{
    /// <summary>
    /// Registers the abortable hub lifetime manager for the given hub.
    /// </summary>
    /// <typeparam name="THub">The hub type.</typeparam>
    /// <param name="services">The service collection.</param>
    public static void AddHub<THub>(this IServiceCollection services) where THub : Hub
    {
        services.AddSingleton<IAbortableHubLifetimeManager<THub>, AbortableHubLifetimeManager<THub>>();
        services.AddSingleton<HubLifetimeManager<THub>>(sp =>
            (HubLifetimeManager<THub>)sp.GetRequiredService<IAbortableHubLifetimeManager<THub>>());
    }
}