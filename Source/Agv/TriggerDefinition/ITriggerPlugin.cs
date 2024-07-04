using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.Common.TriggerDefinition;

/// <summary>
/// Provides an interface for the trigger plugin services registrations
/// </summary>
[PublicAPI]
public interface ITriggerPlugin
{
    /// <summary>
    /// Register dependencies needed by the trigger here using
    /// keyed services with trigger urn as key to avoid dependency collisions
    /// </summary>
    void RegisterServices(IServiceCollection services);
}