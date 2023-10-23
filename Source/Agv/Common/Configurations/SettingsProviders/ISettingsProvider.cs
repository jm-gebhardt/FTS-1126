using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;

/// <summary>
/// Provides settings
/// </summary>
/// <typeparam name="TSettings"></typeparam>
public interface ISettingsProvider<out TSettings> where TSettings : class, ISettings<TSettings>
{
    /// <summary>
    /// Retrieves the settings
    /// </summary>
    /// <returns></returns>
    TSettings GetSettings();

    /// <summary>
    /// Returns true if the settings were found
    /// </summary>
    /// <returns></returns>
    [UsedImplicitly]
    bool DoTheSettingsExist();
}