using Gebhardt.StoreWare.Agv.Common.Configurations;
using Gebhardt.StoreWare.Agv.Common.Configurations.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.Common.Testing.Configurations;

public class FakeSettingsProvider<TSettings> : ISettingsProvider<TSettings> where TSettings : class, ISettings<TSettings>
{
    private readonly Func<TSettings?>? _settingsFunc;
    private readonly TSettings? _settings;

    public FakeSettingsProvider(TSettings? settings)
    {
        _settings = settings;
    }

    public FakeSettingsProvider(Func<TSettings?> settingsFunc)
    {
        _settingsFunc = settingsFunc;
    }

    public TSettings GetSettings() => _settings ?? _settingsFunc?.Invoke() ?? throw new NoSettingsFoundException("fake", typeof(TSettings).Name);

    public bool DoTheSettingsExist() => (_settings ?? _settingsFunc?.Invoke()) != null;
}

public static class FakeSettingsProviderExtensions
{
    [PublicAPI]
    public static IServiceCollection AddFakeSettings<TSettings>(this IServiceCollection services, TSettings? settings)
        where TSettings : class, ISettings<TSettings>
    {
        services.AddSingleton<ISettingsProvider<TSettings>>(new FakeSettingsProvider<TSettings>(settings));
        return services;
    }

    [PublicAPI]
    public static IServiceCollection AddFakeSettings<TSettings>(this IServiceCollection services, Func<TSettings?> settingsFunc)
        where TSettings : class, ISettings<TSettings>
    {
        services.AddSingleton<ISettingsProvider<TSettings>>(new FakeSettingsProvider<TSettings>(settingsFunc));
        return services;
    }
}