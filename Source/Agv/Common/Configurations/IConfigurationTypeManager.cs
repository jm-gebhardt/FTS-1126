using Gebhardt.StoreWare.Agv.Common.Configurations.Models;

namespace Gebhardt.StoreWare.Agv.Common.Configurations
{
    /// <summary>
    /// Holds all configuration types of the initialized assembly(/ies)
    /// </summary>
    public interface IConfigurationTypeManager
    {
        /// <summary>
        /// All found configuration types
        /// </summary>
        IEnumerable<ConfigurationType> ConfigurationTypes { get; }
    }
}