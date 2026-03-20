using System.Reflection;
using AutoMapper.Internal;
using Gebhardt.Acc.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.Common.Extensions;

/// <summary>
/// <inheritdoc cref="EventLogHelper.ConfigureEventLogSettings"/>
/// </summary>
public static class ServiceCollectionExtensions
{
    private const int AutoMapperMaxRecursionDepth = 10;
    
    extension(IServiceCollection services)
    {
        /// <summary>
        /// <inheritdoc cref="EventLogHelper.ConfigureEventLogSettings"/>
        /// </summary>
        /// <param name="executingAssembly">The executing assembly</param>
        public void ConfigureEventLogSettings(Assembly? executingAssembly)
        {
            EventLogHelper.ConfigureEventLogSettings(services, executingAssembly);
        }

        /// <summary>
        /// Add and configure automapper.
        /// </summary>
        /// <param name="currentAssemblies">The assemblies that use automapper.</param>
        public void ConfigureAutoMapper(Assembly[] currentAssemblies)
        {
            services.AddAutoMapper(cfg => cfg.Internal().ForAllMaps((_, map) => map.MaxDepth(AutoMapperMaxRecursionDepth)), currentAssemblies);
        }
    }
}