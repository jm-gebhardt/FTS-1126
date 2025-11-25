using Gebhardt.Acc.Common.Memory;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.Statistics.Extensions;

/// <summary>
/// Provides extension methods for registering scoped cache services.
/// </summary>
public static class ScopedCacheRegistrationExtensions
{
    /// <summary>
    /// Registers scoped cache services for AGVs, enabling scoped caching with specified parameters.
    /// </summary>
    /// <param name="services">The service collection to add the scoped cache to.</param>
    /// <param name="maxItemsPerAgv">The maximum number of items to cache per AGV, before removing the oldest item.</param>
    /// <param name="warnOnOverflow">Indicates whether a warning should be logged when the cache overflows.</param>
    /// <typeparam name="T">The type of the items stored in the cache.</typeparam>
    /// <returns>The updated service collection.</returns>
    [PublicAPI]
    public static IServiceCollection AddAgvScopedCache<T>(this IServiceCollection services, int maxItemsPerAgv, bool warnOnOverflow = true) where T : notnull
    {
        services.AddSingleton(typeof(ICache<Guid, List<T>>), typeof(Cache<Guid, List<T>>));
        return services;
    }
}