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
    where TSaga : class, SagaStateMachineInstance
    where TDbContext : DbContext
    {
        private readonly IDbContextFactory<TDbContext> _dbContextFactory;
        private readonly ISettingsProvider<TSettings> _settingsProvider;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="dbContextFactory"></param>
        /// <param name="settingsProvider"></param>
        public SagaSettingsProvider(IDbContextFactory<TDbContext> dbContextFactory, ISettingsProvider<TSettings> settingsProvider)
        {
            _dbContextFactory = dbContextFactory;
            _settingsProvider = settingsProvider;
        }

        /// <summary>
        /// Retrieves the specific settings out of the Saga. If they are null it will dropdown to the fallback value (from the SettingsProvider), which is most likely the default value from the database
        /// </summary>
        public TSettings GetSettings(Guid correlationId)
        {
            _semaphore.Wait();
            try
            {
                var dbContext = _dbContextFactory.CreateDbContext();
                var saga = dbContext.Set<TSaga>().FirstOrDefault(s => s.CorrelationId == correlationId);
                if (saga is ISagaSetting<TSettings> { Settings: { } settings })
                {
                    return settings;
                }
                else
                {
                    return _settingsProvider.GetSettings();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}