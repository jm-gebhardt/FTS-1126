using Gebhardt.StoreWare.Agv.Common.Configurations;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Saga;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations;

/// <summary>
/// Represents a database source of configuration key/values
/// </summary>
public class DatabaseConfigurationSource<TDbContext> : IConfigurationSource
    where TDbContext : DbContext
{
    /// <summary>
    /// <see cref="ConfigurationTypeManager"/>
    /// </summary>
    public IConfigurationTypeManager ConfigurationTypeManager { get; }

    /// <summary>
    /// The <see cref="SagaPersistenceDbContext"/> to use for retrieving the data
    /// </summary>
    public IDbContextFactory<TDbContext>? DbContextFactory { get; private set; }

    private readonly ILogger _logger;

    /// <summary>
    /// Reloads the configuration key/values when there is a change in entity.
    /// </summary>
    public bool ReloadOnChange { get; init; }

    /// <summary>
    /// Delays the reload of the configuration key/values. Value is expressed in milliseconds.
    /// </summary>
    public int ReloadDelay { get; init; } = 500;

    /// <summary>
    /// Fired once the <see cref="DbContextFactory"/> got updated
    /// </summary>
    public event EventHandler? OnDbContextUpdated;

    /// <inheritdoc cref="DatabaseConfigurationSource{TDbContext}"/>
    /// <param name="configurationTypeManager"></param>
    /// <param name="dbContextFactory">This can be null if it is not yet available. Must be set later in <see cref="UpdateDbContextFactory"/> method!</param>
    /// <param name="logger">The logger to log events</param>
    public DatabaseConfigurationSource(IConfigurationTypeManager configurationTypeManager, IDbContextFactory<TDbContext>? dbContextFactory, ILogger logger)
    {
        ConfigurationTypeManager = configurationTypeManager;
        DbContextFactory = dbContextFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new DatabaseConfigurationProvider<TDbContext>(this, _logger);
    }

    /// <summary>
    /// Is required to be able to set the <see cref="DbContextFactory"/> later
    /// Updates the db context to use
    /// </summary>
    /// <param name="dbContextFactory"></param>
    public void UpdateDbContextFactory(IDbContextFactory<TDbContext> dbContextFactory)
    {
        DbContextFactory = dbContextFactory;
        OnDbContextUpdated?.Invoke(this, EventArgs.Empty);
    }
}