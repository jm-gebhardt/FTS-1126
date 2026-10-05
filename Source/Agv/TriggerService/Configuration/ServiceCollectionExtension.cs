using Gebhardt.Acc.Common.TriggerDefinition;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Gebhardt.Acc.TriggerService.Domain.Services;

namespace Gebhardt.Acc.TriggerService.Configuration;

/// <summary>
/// Provides extension methods for the service collection.
/// </summary>
public static class ServiceCollectionExtension
{
    /// <summary>
    /// Register all dependencies needed by the triggers
    /// </summary>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static IServiceCollection RegisterTriggerDependencies(this IServiceCollection serviceCollection, ILogger initialLogger, TriggerServiceConfiguration triggerServiceConfiguration)
    {
        var plugins = ReflectionService.GetTriggerAssemblies(initialLogger, triggerServiceConfiguration.TriggerFolder)
            .SelectMany(s => s.GetTypes())
            .Where(p => typeof(ITriggerPlugin).IsAssignableFrom(p) && !p.IsInterface);
        foreach (var trigger in plugins)
        {
            var plugin = (ITriggerPlugin)Activator.CreateInstance(trigger)!;
            try
            {
                plugin.RegisterServices(serviceCollection);
            }
            catch(Exception ex) 
            {
                Console.WriteLine(ex.Message);
            }
        }

        return serviceCollection;
    }
}