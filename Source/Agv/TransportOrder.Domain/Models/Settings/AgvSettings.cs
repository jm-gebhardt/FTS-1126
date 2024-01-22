using Gebhardt.StoreWare.Agv.Common.Configurations.Models;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;

/// <summary>
/// The settings for Agv
/// </summary>
public record AgvSettings : SettingsBase<AgvSettings>
{
    /// <summary>Timeout value [ms] to set AGV offline while it is in idle mode</summary>
    public int AgvOfflineTimeoutMsWhileIdle { get; init; } = 15000;

    /// <summary>Timeout value [ms] to set AGV offline while it is in busy mode</summary>
    public int AgvOfflineTimeoutMsWhileBusy { get; init; } = 20000;

    /// <summary>Below this threshold only charge actions will be started.</summary>
    public int BatteryLevelCritical { get; init; } = 15;

    /// <summary>Below this threshold only ActionGroups are started when the system is under high load.</summary>
    public int BatteryLevelLow { get; init; } = 40;

    /// <summary>Once charging is started it will at least charge up to this level.</summary>
    public int MinChargeUp { get; init; } = 60;

    /// <summary>We stop charging at this level actively.</summary>
    public int MaxChargeUp { get; init; } = 80;
}