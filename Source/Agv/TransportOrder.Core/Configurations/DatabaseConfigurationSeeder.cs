using Gebhardt.StoreWare.Agv.Common.Configurations;
using Gebhardt.StoreWare.Agv.Common.Configurations.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations;

/// <summary>
/// Seeds the default settings into the database
/// </summary>
public class DatabaseConfigurationSeeder<TDbContext>
    where TDbContext : DbContext
{
    private readonly IConfigurationTypeManager _configurationTypeManager;
    private readonly TDbContext _dbContext;
    private readonly ILogger<DatabaseConfigurationSeeder<TDbContext>> _logger;

    /// <summary>
    /// Initializes a new <see cref="DatabaseConfigurationSeeder{TDbContext}"/>.
    /// </summary>
    /// <param name="configurationTypeManager"></param>
    /// <param name="dbContext"></param>
    /// <param name="logger"></param>
    public DatabaseConfigurationSeeder(IConfigurationTypeManager configurationTypeManager, TDbContext dbContext, ILogger<DatabaseConfigurationSeeder<TDbContext>> logger)
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
                DbSet<ConfigurationSetting> dbSet = _dbContext.Set<ConfigurationSetting>();
                List<ConfigurationSetting> settings = await dbSet.Where(c => c.ConfigurationType == configurationType.Name).ToListAsync();
                if (!settings.Any())
                {
                    foreach (var configurationTypeProperty in configurationType.Properties.Where(p => p.AutoSeedToDatabase))
                    {
                        string key = configurationTypeProperty.Key;
                        var setting = settings.FirstOrDefault(s => s.Key == key);
                        if (setting == null)
                        {
                            await dbSet.AddAsync(new()
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