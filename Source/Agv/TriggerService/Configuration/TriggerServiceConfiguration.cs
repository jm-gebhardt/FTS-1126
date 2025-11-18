using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace Gebhardt.Acc.TriggerService.Configuration;

/// <summary>
/// Provides a configuration of the trigger service
/// </summary>
[PublicAPI]
public class TriggerServiceConfiguration
{
    /// <summary>
    /// The folder where triggers reside
    /// </summary>
    public string TriggerFolder { get; set; } = "triggers";

    /// <summary>
    /// The interval [ms] for re-loading triggers from a folder.
    /// </summary>
    public int TriggerReloadIntervalMs { get; set; } = 120000;

    /// <summary>
    /// Update the trigger service configuration from a configuration section.
    /// </summary>
    /// <param name="configuration">The configuration section.</param>
    public void UpdateFromConfigurationSection(IConfigurationSection configuration)
    {
        TriggerFolder = configuration.GetValue("FOLDER", "triggers");
        TriggerReloadIntervalMs = configuration.GetValue("RELOAD_INTERVAL_MS", -1);
    }
}