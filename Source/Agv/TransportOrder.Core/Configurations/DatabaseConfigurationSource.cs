using Gebhardt.StoreWare.Agv.Common.Configurations;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations
{
    /// <summary>
    /// Represents a database source of configuration key/values
    /// </summary>
    public class DatabaseConfigurationSource : IConfigurationSource
    {
        /// <summary>
        /// <see cref="ConfigurationTypeManager"/>
        /// </summary>
        public IConfigurationTypeManager ConfigurationTypeManager { get; }

        /// <summary>
        /// The <see cref="SagaPersistenceDbContext"/> to use for retrieving the data
        /// </summary>
        public SagaPersistenceDbContext? DbContext { get; private set; }

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
        /// Fired once the <see cref="DbContext"/> got updated
        /// </summary>
        public event EventHandler? OnDbContextUpdated;

        /// <summary>
        /// <inheritdoc cref="DatabaseConfigurationSource"/>
        /// </summary>
        /// <param name="configurationTypeManager"></param>
        /// <param name="dbContext">This can be null if it is not yet available. Must be set later in <see cref="UpdateDbContext"/> method!</param>
        /// <param name="logger">The logger to log events</param>
        public DatabaseConfigurationSource(IConfigurationTypeManager configurationTypeManager, SagaPersistenceDbContext? dbContext, ILogger logger)
        {
            ConfigurationTypeManager = configurationTypeManager;
            DbContext = dbContext;
            _logger = logger;
        }

        /// <inheritdoc/>
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new DatabaseConfigurationProvider(this, _logger);
        }

        /// <summary>
        /// Is required to be able to set the <see cref="DbContext"/> lately
        /// Updates the db context to use
        /// </summary>
        /// <param name="dbContext"></param>
        public void UpdateDbContext(SagaPersistenceDbContext dbContext)
        {
            DbContext = dbContext;
            OnDbContextUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}