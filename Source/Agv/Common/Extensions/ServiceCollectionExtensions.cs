using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Gebhardt.StoreWare.Agv.Common.Helpers;

namespace Gebhardt.StoreWare.Agv.Common.Extensions;

/// <summary>
/// <inheritdoc cref="EventLogHelper.ConfigureEventLogSettings"/>
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// <inheritdoc cref="EventLogHelper.ConfigureEventLogSettings"/>
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="executingAssembly">The executing assembly</param>
    public static void ConfigureEventLogSettings(this IServiceCollection services, Assembly? executingAssembly)
    {
        EventLogHelper.ConfigureEventLogSettings(services, executingAssembly);
    }
}