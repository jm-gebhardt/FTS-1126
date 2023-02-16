using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Configurations.Models
{
    /// <summary>
    /// The model for the type of configuration holding all properties
    /// </summary>
    public class ConfigurationType
    {
        /// <summary>
        /// <inheritdoc cref="ConfigurationType"/>
        /// </summary>
        public ConfigurationType(Type settingType)
        {
            Name = settingType.Name;
            Type = settingType;
            DefaultInstance = Activator.CreateInstance(settingType)!;

            Properties = settingType.GetProperties().Select(p => new ConfigurationTypeProperty($"{Name}:{p.Name}", p.GetValue(DefaultInstance)?.ToString()));
        }

        /// <summary>
        /// The name of the configuration
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The type used for the configuration
        /// </summary>
        public Type Type { get; }

        /// <summary>
        ///  The properties of the configuration
        /// </summary>
        public IEnumerable<ConfigurationTypeProperty> Properties { get; }

        /// <summary>
        /// An object of the type constructed by using the default ctor with it's default values
        /// </summary>
        [UsedImplicitly]
        public object DefaultInstance { get; }
    }
}
