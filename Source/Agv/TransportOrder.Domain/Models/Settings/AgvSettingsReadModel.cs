using Gebhardt.Acc.Common.Configurations.Models;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Settings;

/// <summary>
/// The settings for an agv (group).
/// </summary>
public record AgvSettingsReadModel :  SettingsBase<AgvSettingsReadModel>
{
    /// <summary>Timeout value [ms] to set AGV offline while it is in idle mode</summary>
    public int AgvOfflineTimeoutMsWhileIdle { get; set; } = -1;

    /// <summary>Timeout value [ms] to set AGV offline while it is in busy mode</summary>
    public int AgvOfflineTimeoutMsWhileBusy { get; set; } = -1;

    /// <summary>Below this threshold [%] only charge actions will be started .</summary>
    public int BatteryLevelCritical { get; set; } = -1;

    /// <summary>Below this threshold [%] only ActionGroups are started when the system is under high load.</summary>
    public int BatteryLevelLow { get; set; } = -1;

    /// <summary>Once charging is started it will at least charge up to this level [%].</summary>
    public int MinChargeUp { get; set; } = -1;

    /// <summary>We stop charging at this level [%].</summary>
    public int MaxChargeUp { get; set; } = -1;

    /// <summary>Charge infinitely, if no order comes in or charging is aborted by the user.</summary>
    public bool InfiniteCharging { get; set; }

    /// <inheritdoc cref="AgvSettingsReadModel" />
    public AgvSettingsReadModel() { }

    /// <inheritdoc cref="AgvSettingsReadModel" />
    public AgvSettingsReadModel(
        int agvOfflineTimeoutMsWhileIdle = -1,
        int agvOfflineTimeoutMsWhileBusy = -1,
        int batteryLevelCritical = -1,
        int batteryLevelLow = -1,
        int minChargeUp = -1,
        int maxChargeUp = -1,
        bool infiniteCharging = false)
    {
        AgvOfflineTimeoutMsWhileIdle = agvOfflineTimeoutMsWhileIdle;
        AgvOfflineTimeoutMsWhileBusy = agvOfflineTimeoutMsWhileBusy;
        BatteryLevelCritical = batteryLevelCritical;
        BatteryLevelLow = batteryLevelLow;
        MinChargeUp = minChargeUp;
        MaxChargeUp = maxChargeUp;
        InfiniteCharging = infiniteCharging;
    }

    /// <summary>
    /// Update the agv settings read model from given new settings.
    /// </summary>
    /// <param name="newSettings">The new settings.</param>
    public void Update(AgvSettingsReadModel newSettings)
    {
        if (newSettings.AgvOfflineTimeoutMsWhileIdle >= 0)
        {
            AgvOfflineTimeoutMsWhileIdle = newSettings.AgvOfflineTimeoutMsWhileIdle;
        }

        if (newSettings.AgvOfflineTimeoutMsWhileBusy >= 0)
        {
            AgvOfflineTimeoutMsWhileBusy = newSettings.AgvOfflineTimeoutMsWhileBusy;
        }

        if (newSettings.BatteryLevelCritical >= 0)
        {
            BatteryLevelCritical = newSettings.BatteryLevelCritical;
        }

        if (newSettings.BatteryLevelLow >= 0)
        {
            BatteryLevelLow = newSettings.BatteryLevelLow;
        }

        if (newSettings.MinChargeUp >= 0)
        {
            MinChargeUp = newSettings.MinChargeUp;
        }

        if (newSettings.MaxChargeUp >= 0)
        {
            MaxChargeUp = newSettings.MaxChargeUp;
        }

        InfiniteCharging = newSettings.InfiniteCharging;
    }

    /// <summary>
    /// Get name and value of all valid properties.
    /// </summary>
    /// <returns>Collection of (Name, Value) of valid properties.</returns>
    public ICollection<(string Name, object? Value)> GetValidPropertiesInformation()
    {
        return GetType()
            .GetProperties()
            .Where(p => (p.PropertyType == typeof(int) &&
                        p.GetValue(this) is IComparable comparable &&
                        comparable.CompareTo(0) >= 0) || p.PropertyType == typeof(bool))
            .Select(p => (p.Name, p.GetValue(this))).ToList();
    }
}