using Gebhardt.StoreWare.Agv.Common.Configurations;
using Gebhardt.StoreWare.Agv.Common.Configurations.ConfigurationProviders;
using Gebhardt.StoreWare.Agv.Common.Configurations.Extensions;
using Gebhardt.StoreWare.Agv.Common.Configurations.Models;
using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Logging;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Configuration;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Configurations;

/// <summary>
/// Test fixture to test ConfigurationProvider
/// </summary>
public class ConfigurationProviderTestFixture
{
    public record TestSettings : SettingsBase<TestSettings>
    {
        public enum TestEnum
        {
            Test1
        }

        public bool BoolValue { get; init; } = true;

        public int IntValue { get; } = 1;

        public static TestEnum EnumValue => TestEnum.Test1;
    }

    private readonly IHostBuilder _hostBuilder;
    private IHost _host = null!;

    private DatabaseConfigurationSource<TransportOrderDbContext>? _databaseConfigurationSource;
    private readonly ConfigurationTypeManager _configurationTypeManager = new();

    private IDbContextFactory<TransportOrderDbContext> DbFactory => _host.Services.GetRequiredService<IDbContextFactory<TransportOrderDbContext>>();

    public IConfiguration Configuration => _host.Services.GetRequiredService<IConfiguration>();

    public ISettingsProvider<TSettings> GetSettingsProvider<TSettings>() where TSettings : class, ISettings<TSettings>
    {
        return _host.Services.GetRequiredService<ISettingsProvider<TSettings>>();
    }

    public IEnumerable<ConfigurationType> ConfigurationTypes => _configurationTypeManager.ConfigurationTypes;

    /// <summary>
    /// <inheritdoc cref="ConfigurationProviderTestFixture"/>
    /// </summary>
    public ConfigurationProviderTestFixture()
    {
        _hostBuilder = Host.CreateDefaultBuilder();
        _configurationTypeManager.InitializeConfigurationTypes();
    }

    public ConfigurationProviderTestFixture AddDatabase()
    {
        _hostBuilder.ConfigureServices(s =>
        {
            s.AddDbContextFactory<TransportOrderDbContext>(options => { options.UseInMemoryDatabase(Guid.NewGuid().ToString()); }
            );
        });
        return this;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public ConfigurationProviderTestFixture AddDatabaseConfigurationProvider(int reloadDelay = 0, bool reloadOnChange = true)
    {
        var logger = InitialLogger.Create<DatabaseConfigurationProviderTest>();

        _hostBuilder.ConfigureAppConfiguration((_, config) =>
        {
            _databaseConfigurationSource = config.AddDatabaseConfiguration<TransportOrderDbContext>(_configurationTypeManager, logger, reloadDelay, reloadOnChange);
            _databaseConfigurationSource.ShouldNotBeNull();
        });
        return this;
    }

    /// <summary>
    /// Registers the <see cref="DefaultConfigurationProvider"/> in the app configuration
    /// </summary>
    public ConfigurationProviderTestFixture AddDefaultConfigurationProvider()
    {
        _hostBuilder.ConfigureAppConfiguration((_, builder) => { builder.Add(new DefaultConfigurationSource(_configurationTypeManager)); });
        return this;
    }

    /// <summary>
    /// Registers the <see cref="SettingsProvider{TSettings}"/> in the service collection
    /// </summary>
    /// <typeparam name="TSettings"></typeparam>
    public ConfigurationProviderTestFixture AddSettingsProvider<TSettings>() where TSettings : class, ISettings<TSettings>
    {
        _hostBuilder.ConfigureServices(sp => sp.AddSettingsProviders(_configurationTypeManager));
        return this;
    }

    public ConfigurationProviderTestFixture AddSeeder()
    {
        // Scoped
        _hostBuilder.ConfigureServices(sp =>
        {
            sp.AddScoped<IConfigurationTypeManager>(_ => _configurationTypeManager);
            sp.AddScoped<DatabaseConfigurationSeeder<TransportOrderDbContext>>();
        });
        return this;
    }

    /// <summary>
    /// Builds the service provider based on the service collection, in order to register lazy services
    /// </summary>
    public ConfigurationProviderTestFixture Build()
    {
        _host = _hostBuilder.Build();
        _databaseConfigurationSource?.UpdateDbContextFactory(DbFactory);
        return this;
    }

    /// <summary>
    /// Seeds the configuration types to the database
    /// </summary>
    public async Task SeedConfigurationTypesAsync()
    {
        using var scope = _host.Services.CreateScope();
        var dbConfigurationSeeder = scope.ServiceProvider.GetRequiredService<DatabaseConfigurationSeeder<TransportOrderDbContext>>();
        await dbConfigurationSeeder.SeedDefaultConfigurationAsync();
    }

    /// <summary>
    /// Gets the <see cref="ConfigurationSetting"/> from the database
    /// </summary>
    /// <returns></returns>
    public async Task<IList<ConfigurationSetting>> GetConfigurationSettingsAsync()
    {
        IDbContextFactory<TransportOrderDbContext> dbContextFactory = DbFactory;
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Set<ConfigurationSetting>().ToListAsync();
    }

    public async Task UpdateConfigurationSettingAsync(string key, string value, string configurationType)
    {
        await using var ctx = await DbFactory.CreateDbContextAsync();
        DbSet<ConfigurationSetting> dbSet = ctx.Set<ConfigurationSetting>();
        var configuration = dbSet.FirstOrDefault(c => c.Key == key);

        if (configuration == null)
        {
            await dbSet.AddAsync(new ConfigurationSetting { Key = key, Value = value, ConfigurationType = configurationType });
        }
        else
        {
            configuration.Value = value;
            dbSet.Update(configuration);
        }

        await ctx.SaveChangesAsync();

        // Force the Configuration source to reload from database
        _databaseConfigurationSource!.UpdateDbContextFactory(DbFactory);
    }
}