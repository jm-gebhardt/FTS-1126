using Gebhardt.StoreWare.Agv.Common.Configurations;
using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations
{
    /// <summary>
    /// Extensions for building the configuration provider
    /// </summary>
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// Injects the default configuration provider
        /// </summary>
        /// <param name="builder">The builder to build the application configuration</param>
        /// <param name="configurationTypeManager"></param>
        /// <returns>The <see cref="IConfigurationBuilder"/> object</returns>
        [UsedImplicitly]
        public static IConfigurationBuilder AddDefaultConfiguration(this IConfigurationBuilder builder, IConfigurationTypeManager configurationTypeManager)
        {
            return builder.Add(new DefaultConfigurationSource(configurationTypeManager));
        }

        /// <summary>
        /// Injects the database configuration provider. You are responsible of setting the db context of the DatabaseConfigurationSource once it is available!
        /// </summary>
        /// <param name="builder">The builder to build the application configuration</param>
        /// <param name="configurationTypeManager"></param>
        /// <param name="logger">The logger to log events</param>
        /// <param name="reloadDelay">Delays the reload of configuration. The value is expressed in milliseconds</param>
        /// <param name="reloadOnChange">Whether to reload the configuration from database when there is a change in the entity</param>
        /// <returns>The <see cref="IConfigurationBuilder"/> object</returns>
        public static DatabaseConfigurationSource AddDatabaseConfiguration(this IConfigurationBuilder builder, IConfigurationTypeManager configurationTypeManager, ILogger logger, int reloadDelay = 500, bool reloadOnChange = false)
        {
            var databaseConfigurationSource = new DatabaseConfigurationSource(configurationTypeManager, null, logger)
            {
                ReloadOnChange = reloadOnChange,
                ReloadDelay = reloadDelay
            };
            builder.AddDatabaseConfigurationSource(databaseConfigurationSource);
            return databaseConfigurationSource;
        }

        /// <summary>
        /// Injects the in-memory database configuration provider
        /// </summary>
        /// <param name="builder">The builder to build the application configuration</param>
        /// <param name="databaseConfigurationSource"></param>
        /// <returns></returns>
        [UsedImplicitly]
        public static IConfigurationBuilder AddDatabaseConfigurationSource(this IConfigurationBuilder builder, DatabaseConfigurationSource databaseConfigurationSource)
        {
            return builder.Add(databaseConfigurationSource);
        }
    }
}