using Gebhardt.StoreWare.Agv.Common.Configurations;
using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations;

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
    /// <param name="index">Index position where the configuration source is added using insert</param>
    /// <returns>The <see cref="IConfigurationBuilder"/> object</returns>
    [UsedImplicitly]
    public static IConfigurationBuilder AddDefaultConfiguration(this IConfigurationBuilder builder, IConfigurationTypeManager configurationTypeManager, int index=0)
    {
        // DefaultConfigurationProvider should be at index 0 of the provider layer, so that environment variable can overwrite the hardcoded values of a ISettings
        builder.Sources.Insert(index, new DefaultConfigurationSource(ConfigurationTypeManager.Instance));
        return builder;
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
    public static DatabaseConfigurationSource<TDbContext> AddDatabaseConfiguration<TDbContext>(this IConfigurationBuilder builder, IConfigurationTypeManager configurationTypeManager, ILogger logger, int reloadDelay = 500, bool reloadOnChange = false)
        where TDbContext : DbContext
    {
        var databaseConfigurationSource = new DatabaseConfigurationSource<TDbContext>(configurationTypeManager, null, logger)
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
    public static IConfigurationBuilder AddDatabaseConfigurationSource<TDbContext>(this IConfigurationBuilder builder, DatabaseConfigurationSource<TDbContext> databaseConfigurationSource)
        where TDbContext : DbContext
    {
        return builder.Add(databaseConfigurationSource);
    }
}