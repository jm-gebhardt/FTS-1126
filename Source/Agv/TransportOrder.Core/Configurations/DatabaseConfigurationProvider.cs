using Gebhardt.StoreWare.Agv.Common.Configurations;
using Gebhardt.StoreWare.Agv.Common.Configurations.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations
{
    /// <summary>
    /// Implementation of a database configuration provider
    /// </summary>
    public class DatabaseConfigurationProvider : ConfigurationProvider
    {
        private readonly DatabaseConfigurationSource _source;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _lockSemaphoreSlim = new(1, 1);

        /// <summary>
        /// <inheritdoc cref="DatabaseConfigurationProvider" />
        /// </summary>
        /// <param name="source">The database configuration source</param>
        /// <param name="logger">The logger to log events</param>
        public DatabaseConfigurationProvider(DatabaseConfigurationSource source, ILogger logger)
        {
            _source = source;
            _logger = logger;

            _source.OnDbContextUpdated += (_, _) => Load();
            if (_source.ReloadOnChange)
            {
                EntityChangeObserver.Instance.Changed += EntityChangeObserverChanged;
            }
        }

        private void EntityChangeObserverChanged(object? sender, EventArgs e)
        {
            Thread.Sleep(_source.ReloadDelay);
            Load();
        }

        /// <summary>
        /// Loads the key / value pairs in the Data dictionary
        /// </summary>
        public override void Load()
        {
            var context = _source.DbContext;
            if (context == null)
            {
                // context not yet set. Will be set with UpdateDbContext method
                return;
            }

            try
            {
                _lockSemaphoreSlim.Wait();
                var dbContext = context;
                dbContext.Database.EnsureCreated();

                Data = new Dictionary<string, string>()!;

                List<ConfigurationSetting> configurationSettings = dbContext.ConfigurationSettings.ToList();
                foreach (var setting in configurationSettings)
                {
                    var configuration = _source.ConfigurationTypeManager.GetConfigurationTypeByTypeName(setting.ConfigurationType);
                    if (configuration == null)
                    {
                        _logger.LogError("Configuration setting unknown: {ConfigurationType}", setting.ConfigurationType);
                        continue;
                    }

                    string key = setting.Key;
                    var configurationTypeProperty = configuration.Properties.SingleOrDefault(s => s.Key == key);
                    if (configurationTypeProperty == null)
                    {
                        _logger.LogError("Settings property using the key '{@ConfigurationPropertyKey}' not found", key);
                        continue;
                    }

                    Data.Add(key, setting.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(Load));
                throw;
            }
            finally
            {
                _lockSemaphoreSlim.Release();
            }
        }
    }
}