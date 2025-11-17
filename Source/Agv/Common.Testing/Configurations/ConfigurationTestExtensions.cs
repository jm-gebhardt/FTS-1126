using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gebhardt.Acc.Common.Testing.Configurations;

/// <summary>
/// Contains extension methods to test configurations.
/// </summary>
public static class ConfigurationTestExtensions
{
    /// <summary>
    /// Finds all the captive dependencies of a host builder. Captive dependencies are non-singletons injected into singletons unless they are annotated with <see cref="CanBeCaptiveDependency"/>.
    /// Also builds the host builder.
    /// </summary>
    [PublicAPI]
    public static List<Tuple<Type, Type>> FindCaptiveDependencies(this IHostBuilder hostBuilder)
    {
        List<Tuple<Type, Type>> result = [];
        IServiceCollection serviceCollection = null!;
        hostBuilder.ConfigureServices(sc =>
        {
            serviceCollection = sc;
        });

        hostBuilder.Build();

        List<ServiceDescriptor> singletons = serviceCollection.Where(s => s.Lifetime == ServiceLifetime.Singleton && s.ServiceType.Namespace?.StartsWith("Gebhardt") == true).ToList();

        foreach (var singleton in singletons)
        {
            List<ServiceDescriptor> captives = (singleton.ImplementationType ?? singleton.ServiceType)
                .GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Select(p => p.ParameterType)
                .SelectMany(p => serviceCollection.Where(s => s.ServiceType.IsAssignableTo(p)))
                .Where(s => s.Lifetime != ServiceLifetime.Singleton)
                .Where(s => 
                    (s.ImplementationType != null || s is { IsKeyedService: true, KeyedImplementationType: not null }) &&
                    !(Attribute.IsDefined((s.ImplementationType ?? s.KeyedImplementationType)!, typeof(CanBeCaptiveDependency)) && s.Lifetime == ServiceLifetime.Transient))
                .Distinct()
                .ToList();
            result.AddRange(captives.Select(c => new Tuple<Type, Type>(singleton.ServiceType, c.ServiceType)));
        }

        return result;
    }
}