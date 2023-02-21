using Gebhardt.StoreWare.Agv.Common.Configurations;
using Gebhardt.StoreWare.Agv.Common.Configurations.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations
{
    /// <summary>
    /// Seeds the default settings into the database
    /// </summary>
    public class DatabaseConfigurationSeeder
    {
        private readonly IConfigurationTypeManager _configurationTypeManager;
        private readonly SagaPersistenceDbContext _dbContext;
        private readonly ILogger<DatabaseConfigurationSeeder> _logger;

        /// <summary>
        /// <inheritdoc cref="DatabaseConfigurationSeeder"/>
        /// </summary>
        /// <param name="configurationTypeManager"></param>
        /// <param name="dbContext"></param>
        /// <param name="logger"></param>
        public DatabaseConfigurationSeeder(IConfigurationTypeManager configurationTypeManager, SagaPersistenceDbContext dbContext, ILogger<DatabaseConfigurationSeeder> logger)
        {
            _configurationTypeManager = configurationTypeManager;
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Populates the default value for all found configuration types to the database. If the key is already existing it won't get touched.
        /// </summary>
        public async Task SeedDefaultConfigurationAsync()
        {
            try
            {
                foreach (var configurationType in _configurationTypeManager.ConfigurationTypes.Where(t => t.AutoSeedToDatabase))
                {
                    List<ConfigurationSetting> settings = _dbContext.ConfigurationSettings.Where(c => c.ConfigurationType == configurationType.Name).ToList();
                    if (!settings.Any())
                    {
                        foreach (var configurationTypeProperty in configurationType.Properties.Where(p => p.AutoSeedToDatabase))
                        {
                            string key = configurationTypeProperty.Key;
                            var setting = settings.FirstOrDefault(s => s.Key == key);
                            if (setting == null)
                            {
                                await _dbContext.ConfigurationSettings.AddAsync(new()
                                {
                                    Key = key,
                                    Value = configurationTypeProperty.Value,
                                    ConfigurationType = configurationType.Name
                                });
                            }
                            else
                            {
                                _logger.LogInformation("Default settings already existing, don't store new default values");
                            }
                        }
                    }
                }

                if (_dbContext.ChangeTracker.HasChanges())
                {
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(SeedDefaultConfigurationAsync));
            }
        }
    }
}