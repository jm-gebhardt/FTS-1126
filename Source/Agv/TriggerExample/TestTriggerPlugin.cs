using Gebhardt.Acc.Common.TriggerDefinition;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.TriggerExample;

/// <inheritdoc cref="ITriggerPlugin"/>>
[UsedImplicitly]
public class TestTriggerPlugin : ITriggerPlugin
{
    /// <summary>
    /// Register dependencies needed by the trigger here using
    /// keyed services to avoid dependency collisions
    /// </summary>
    /// <param name="services"></param>
    public void RegisterServices(IServiceCollection services)
    {
        Console.WriteLine("Registering test trigger plugin dependencies");
    }
}