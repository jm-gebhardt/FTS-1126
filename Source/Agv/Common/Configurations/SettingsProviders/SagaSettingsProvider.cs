using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders
{
    /// <summary>
    /// Uses the settings from the Saga if possible, otherwise will use the normal SettingsProvider
    /// </summary>
    /// <typeparam name="TSettings">The settings type</typeparam>
    /// <typeparam name="TSaga">The type of the saga in which the setting is stored</typeparam>
    /// <typeparam name="TDbContext">The dbContext of the Saga</typeparam>
    public class SagaSettingsProvider<TSettings, TSaga, TDbContext> : ISagaSettingsProvider<TSettings, TSaga>
    where TSettings : class, ISettings<TSettings>
    where TSaga : class, SagaStateMachineInstance, ISagaDefinedSetting<ISagaSettings<TSettings>, TSettings>
    where TDbContext : DbContext
    {
        private readonly IDbContextFactory<TDbContext> _dbContextFactory;
        private readonly ISettingsProvider<TSettings> _settingsProvider;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="dbContextFactory">The context factory for the <see ref="TDbContext"/></param>
        /// <param name="settingsProvider">The settings provider for <see ref="TSettings"/></param>
        public SagaSettingsProvider(IDbContextFactory<TDbContext> dbContextFactory, ISettingsProvider<TSettings> settingsProvider)
        {
            _dbContextFactory = dbContextFactory;
            _settingsProvider = settingsProvider;
        }

        /// <summary>
        /// Retrieves the specific settings out of the Saga. If a value of a property is null it will dropdown to property value in the fallback layer (from the SettingsProvider), which is most likely the default value from the database
        /// </summary>
        public TSettings GetSettings(Guid correlationId)
        {
            _semaphore.Wait();
            try
            {
                // default settings from configuration table or hardcoded in settings class
                var settings = _settingsProvider.GetSettings();

                using var dbContext = _dbContextFactory.CreateDbContext();
                var saga = dbContext.Set<TSaga>().FirstOrDefault(s => s.CorrelationId == correlationId);

                if (saga is ISagaDefinedSetting<ISagaSettings<TSettings>, TSettings> { Settings: { } specificSettings })
                {
                    settings = OverwriteSpecificValues(settings, specificSettings);
                }

                return settings;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private static TSettings OverwriteSpecificValues(TSettings settings, ISagaSettings<TSettings> specificSettings)
        {
            foreach (var explicitSetProperty in specificSettings.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
            {
                string propertyName = explicitSetProperty.Name;
                var property = settings.GetType().GetProperty(propertyName);
                object? explicitSetValue = explicitSetProperty.GetValue(specificSettings);

                if (property == null)
                {
                    throw new ArgumentException($"{propertyName} is defined in Saga, but not in settings class!");
                }

                if (explicitSetValue != null)
                {
                    // set explicit set value to settings
                    property.SetValue(settings, explicitSetValue);
                }
            }

            return settings;
        }
    }
}