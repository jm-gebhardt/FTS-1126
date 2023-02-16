using System.Reflection;
using Gebhardt.StoreWare.Agv.Common.Configurations.Models;

namespace Gebhardt.StoreWare.Agv.Common.Configurations
{
    /// <summary>
    /// Manages the configuration types from different assemblies
    /// </summary>
    public sealed class ConfigurationTypeManager : IConfigurationTypeManager
    {
        private static readonly Lazy<ConfigurationTypeManager> _lazy = new(() => new ConfigurationTypeManager());
        private IEnumerable<ConfigurationType>? _configurationTypes;

        private ConfigurationTypeManager()
        {

        }

        /// <summary>
        /// Singleton instance of <see cref="ConfigurationTypeManager"/>.
        /// </summary>
        public static ConfigurationTypeManager Instance => _lazy.Value;

        /// <summary>
        /// Initializes the <see cref="ConfigurationTypes"/> property
        /// </summary>
        public void InitializeConfigurationTypes()
        {
            _configurationTypes ??= GetConfigurationTypes(AppDomain.CurrentDomain.GetAssemblies());
        }

        /// <inheritdoc />
        public IEnumerable<ConfigurationType> ConfigurationTypes => _configurationTypes ?? throw new Exception($"{nameof(ConfigurationTypeManager)} has not been initialized");

        private static IEnumerable<ConfigurationType> GetConfigurationTypes(params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                IEnumerable<ConfigurationType> configurationTypes = assembly.GetTypes()
                    .Where(t => t.IsAssignableTo(typeof(ISettings)) && !t.IsInterface)
                    .Select(t => new ConfigurationType(t));

                foreach (var configurationType in configurationTypes)
                {
                    yield return configurationType;
                }
            }
        }
    }
}