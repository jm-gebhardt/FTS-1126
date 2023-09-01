using Gebhardt.StoreWare.Agv.Common.Configurations;
using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TransportOrder.Domain.Models.Settings;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Configurations;

/// <summary>
/// Extension methods for IServiceCollection for registering settings
/// </summary>
public static class ConfigureSettingsFixture
{
    /// <summary>
    /// Registers Mock which is returning the given <see cref="AgvSettings"/>
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/></param>
    /// <param name="settings">The settings to register. If null then the default value of the settings get used</param>
    /// <returns><see cref="IServiceCollection"/></returns>
    [UsedImplicitly]
    public static IServiceCollection AddAgvSettings(this IServiceCollection services, AgvSettings? settings = null)
    {
        var settingsProviderMock = new Mock<ISettingsProvider<AgvSettings>>();
        settingsProviderMock.Setup(m => m.GetSettings()).Returns(() => settings ?? new AgvSettings());
        services.AddSingleton(settingsProviderMock.Object);

        return services;
    }

    /// <summary>
    /// Registers Mock which is returning the given <see cref="AddSettings{TSettings}"/>
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/></param>
    /// <param name="settings">The settings to register. If null then the default value of the settings get used</param>
    /// <returns><see cref="IServiceCollection"/></returns>
    [UsedImplicitly]
    public static IServiceCollection AddSettings<TSettings>(this IServiceCollection services, TSettings? settings = null) where TSettings : class, ISettings<TSettings>
    {
        var settingsProviderMock = new Mock<ISettingsProvider<TSettings>>();
        settingsProviderMock.Setup(m => m.GetSettings()).Returns(() => settings ?? Activator.CreateInstance<TSettings>());
        services.AddSingleton(settingsProviderMock.Object);

        return services;
    }
}