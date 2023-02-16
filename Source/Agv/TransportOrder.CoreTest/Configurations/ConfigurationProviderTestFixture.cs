using Gebhardt.StoreWare.Agv.Common.Configurations;
using Gebhardt.StoreWare.Agv.Common.Configurations.ConfigurationProviders;
using Gebhardt.StoreWare.Agv.Common.Configurations.Extensions;
using Gebhardt.StoreWare.Agv.Common.Configurations.Models;
using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Logging;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Configurations
{
    /// <summary>
    /// Test fixture to test ConfigurationProvider
    /// </summary>
    public class ConfigurationProviderTestFixture
    {
        [UsedImplicitly]
        private class DbContextOptionsHelper<TDbContext> where TDbContext : DbContext
        {
            public static DbContextOptions<TDbContext> GetOptions(string databaseName)
            {
                var dbContextOptionsBuilder = new DbContextOptionsBuilder<TDbContext>();
                dbContextOptionsBuilder.UseInMemoryDatabase(databaseName);
                return dbContextOptionsBuilder.Options;
            }
        }

        public class DbSetSagaPersistenceDbContext : SagaPersistenceDbContext
        {
            // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
            public DbSet<AgvSaga> AgvSagas { get; set; } = null!;

            public DbSetSagaPersistenceDbContext(string databaseName) : base(DbContextOptionsHelper<SagaPersistenceDbContext>.GetOptions(databaseName)) { }
        }

        private readonly string _databaseName = Guid.NewGuid().ToString();
        private const string AgvId = "AGV1";
        private const string MapName = "Map#1";
        private readonly Guid _correlationId = AgvId.ToGuid();
        private readonly AgvSaga _agvSaga;
        private readonly IHostBuilder _hostBuilder;
        private IHost _host = null!;
        private DatabaseConfigurationSource? _databaseConfigurationSource;

        public IDbContextFactory<DbSetSagaPersistenceDbContext> DbSetSagaPersistenceDbContextFactory => _host.Services.GetRequiredService<IDbContextFactory<DbSetSagaPersistenceDbContext>>();
        public IConfiguration Configuration => _host.Services.GetRequiredService<IConfiguration>();
        public SagaSettingsProvider<TSettings, TSaga, DbSetSagaPersistenceDbContext> GetSagaSettingsProvider<TSettings, TSaga>()
        where TSettings : class, ISettings<TSettings>
        where TSaga : class, SagaStateMachineInstance => _host.Services.GetRequiredService<SagaSettingsProvider<TSettings, TSaga, DbSetSagaPersistenceDbContext>>();
        public ISettingsProvider<TSettings> GetSettingsProvider<TSettings>() where TSettings : class, ISettings<TSettings> => _host.Services.GetRequiredService<ISettingsProvider<TSettings>>();
        public static IEnumerable<ConfigurationType> ConfigurationTypes => ConfigurationTypeManager.Instance.ConfigurationTypes;

        /// <summary>
        /// <inheritdoc cref="ConfigurationProviderTestFixture"/>
        /// </summary>
        public ConfigurationProviderTestFixture()
        {
            _hostBuilder = Host.CreateDefaultBuilder();

            ConfigurationTypeManager.Instance.InitializeConfigurationTypes();

            _agvSaga = new AgvSaga(_correlationId, AgvId, "TestState",
            new AgvStatus(Guid.NewGuid(), "123", true, true, 10, false, false),
            new AgvGraph(Guid.NewGuid(),
                new AgvGraph.Node[] { new("id") },
                new AgvGraph.Edge[] { new("from", "to") },
            MapName.ToGuid(),
            MapName));

        }

        /// <summary>
        /// Registers the <see cref="DbSetSagaPersistenceDbContext"/> in the service collection
        /// </summary>
        public ConfigurationProviderTestFixture AddDbSetSagaDbContext()
        {
            _hostBuilder.ConfigureServices(sp => sp.AddDbContextFactory<DbSetSagaPersistenceDbContext>());
            _hostBuilder.ConfigureServices(sp => sp.AddSingleton(_databaseName));
            _hostBuilder.ConfigureServices(sp => sp.AddDbContext<DbSetSagaPersistenceDbContext>());
            _hostBuilder.ConfigureServices(sp => sp.AddSingleton<DatabaseConfigurationSeeder>(s =>
            {
                var dbContext = s.GetService<DbSetSagaPersistenceDbContext>()!;
                var logger = s.GetService<ILogger<DatabaseConfigurationSeeder>>()!;
                return new DatabaseConfigurationSeeder(ConfigurationTypeManager.Instance, dbContext, logger);
            }));

            return this;
        }

        /// <summary>
        /// Registers the <see cref="DatabaseConfigurationProvider"/> in the app configuration
        /// </summary>
        public ConfigurationProviderTestFixture AddDatabaseConfigurationProvider(int reloadDelay = 0, bool reloadOnChange = true)
        {
            var logger = InitialLogger.Create<DatabaseConfigurationProviderTest>();
            IConfigurationTypeManager configurationTypeManager = ConfigurationTypeManager.Instance;

            _databaseConfigurationSource = new DatabaseConfigurationSource(configurationTypeManager, null, logger)
            {
                ReloadOnChange = reloadOnChange,
                ReloadDelay = reloadDelay
            };

            _hostBuilder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddDatabaseConfigurationSource(_databaseConfigurationSource);
            });

            _hostBuilder.ConfigureServices(sp => sp.AddSingleton(_databaseConfigurationSource));

            return this;
        }

        /// <summary>
        /// Registers the <see cref="DefaultConfigurationProvider"/> in the app configuration
        /// </summary>
        public ConfigurationProviderTestFixture AddDefaultConfigurationProvider()
        {
            _hostBuilder.ConfigureAppConfiguration((_, config) => config.AddDefaultConfiguration(ConfigurationTypeManager.Instance));
            return this;
        }

        /// <summary>
        /// Registers the <see cref="SettingsProvider{TSettings}"/> in the service collection
        /// </summary>
        /// <typeparam name="TSettings"></typeparam>
        public ConfigurationProviderTestFixture AddSettingsProvider<TSettings>() where TSettings : class, ISettings<TSettings>
        {
            _hostBuilder.ConfigureServices(sp => sp.AddSettingsProviders(ConfigurationTypeManager.Instance));
            return this;
        }

        /// <summary>
        /// Registers the <see cref="SagaSettingsProvider{TSettings, TSaga, DbSetSagaPersistenceDbContext}"/> in the service collection
        /// </summary>
        public ConfigurationProviderTestFixture AddSagaSettingsProvider<TSettings, TSaga>()
        where TSettings : class, ISettings<TSettings>
        where TSaga : class, SagaStateMachineInstance
        {
            _hostBuilder.ConfigureServices(sp => sp.AddTransient<SagaSettingsProvider<TSettings, TSaga, DbSetSagaPersistenceDbContext>>(p =>
            {
                var dbContext = p.GetService<DbSetSagaPersistenceDbContext>()!;
                var settingsProvider = p.GetRequiredService<ISettingsProvider<TSettings>>();

                return new SagaSettingsProvider<TSettings, TSaga, DbSetSagaPersistenceDbContext>(dbContext, settingsProvider);
            }));
            return this;
        }

        /// <summary>
        /// Seeds the configuration types to the database
        /// </summary>
        public async Task SeedConfigurationTypesAsync()
        {
            var dbConfigurationSeeder = _host.Services.GetRequiredService<DatabaseConfigurationSeeder>();
            await dbConfigurationSeeder.SeedDefaultConfigurationAsync();
        }

        /// <summary>
        /// Builds the service provider based on the service collection, in order to register lazy services
        /// </summary>
        public ConfigurationProviderTestFixture Build()
        {
            _host = _hostBuilder.Build();
            var source = _host.Services.GetService<DatabaseConfigurationSource>();
            source?.UpdateDbContext(DbSetSagaPersistenceDbContextFactory.CreateDbContext());
            return this;
        }

        /// <summary>
        /// Gets the <see cref="ConfigurationSetting"/> from the database
        /// </summary>
        /// <returns></returns>
        public async Task<IList<ConfigurationSetting>> GetConfigurationSettingsAsync()
        {
            IDbContextFactory<DbSetSagaPersistenceDbContext> dbContextFactory = DbSetSagaPersistenceDbContextFactory;
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            return await dbContext.ConfigurationSettings.ToListAsync();
        }

        /// <summary>
        /// Adds the <see cref="AgvSaga"/> settings
        /// </summary>
        /// <param name="agvOfflineTimeoutMsWhileBusy"></param>
        /// <param name="agvOfflineTimeoutMsWhileIdle"></param>
        public void AddAgvSagaSettings(int agvOfflineTimeoutMsWhileBusy, int agvOfflineTimeoutMsWhileIdle)
        {
            _agvSaga.Settings = new()
            {
                AgvOfflineTimeoutMsWhileBusy = agvOfflineTimeoutMsWhileBusy,
                AgvOfflineTimeoutMsWhileIdle = agvOfflineTimeoutMsWhileIdle
            };
        }

        /// <summary>
        /// Creates an <see cref="AgvSaga"/> record
        /// </summary>
        public async Task CreateAgvSagaAsync()
        {
            IDbContextFactory<DbSetSagaPersistenceDbContext> dbContextFactory = DbSetSagaPersistenceDbContextFactory;
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            await dbContext.Database.EnsureCreatedAsync();

            await dbContext.AgvSagas.AddAsync(_agvSaga);
            await dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Gets the <see cref="AgvSaga"/> record
        /// </summary>
        public async Task<AgvSaga> GetAgvSagaAsync()
        {
            IDbContextFactory<DbSetSagaPersistenceDbContext> dbContextFactory = DbSetSagaPersistenceDbContextFactory;
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var restoredAgvSaga = await dbContext.AgvSagas.FirstAsync();

            return restoredAgvSaga;
        }
    }
}