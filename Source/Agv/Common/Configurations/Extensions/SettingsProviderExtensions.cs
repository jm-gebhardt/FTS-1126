using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.Common.Configurations.Extensions
{
    /// <summary>
    /// Extension methods for SettingsProviders
    /// </summary>
    public static class SettingsProviderExtensions
    {
        /// <summary>
        /// Register SettingsProvider for all ConfigurationTypes from configurationTypeManager
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configurationTypeManager"></param>
        /// <returns></returns>
        [UsedImplicitly]
        public static IServiceCollection AddSettingsProviders(this IServiceCollection services, ConfigurationTypeManager configurationTypeManager)
        {
            foreach (var configurationType in configurationTypeManager.ConfigurationTypes)
            {
                var settingsProviderServiceType = typeof(ISettingsProvider<>).MakeGenericType(configurationType.Type);
                var settingsProviderImplementationType = typeof(SettingsProvider<>).MakeGenericType(configurationType.Type);
                services.AddTransient(settingsProviderServiceType, settingsProviderImplementationType);
            }

            return services;
        }
    }
}