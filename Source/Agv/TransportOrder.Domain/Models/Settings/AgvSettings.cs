using Gebhardt.StoreWare.Agv.Common.Configurations.Models;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;

/// <summary>
/// The settings for Agv
/// </summary>
public record AgvSettings : SettingsBase<AgvSettings>
{
    /// <summary>
    /// Timeout value [ms] to set AGV offline while it is in idle mode
    /// </summary>
    public int AgvOfflineTimeoutMsWhileIdle { get; init; } = 15000;

    /// <summary>
    /// Timeout value [ms] to set AGV offline while it is in busy mode
    /// </summary>
    public int AgvOfflineTimeoutMsWhileBusy { get; init; } = 20000;

    /// <summary>Critical battery level</summary>
    public int BatteryLevelCritical { get; init; } = 15;

    /// <summary>Low battery level</summary>
    public int BatteryLevelLow { get; init; } = 40;

    /// <summary>Good battery level</summary>
    [UsedImplicitly]
    public int BatteryLevelGood { get; init; } = 60;

    /// <summary>At this threshold we stop the charging.</summary>
    [UsedImplicitly]
    public int MaxChargeUp { get; init; } = 80;

    /// <summary>Full battery level</summary>
    [UsedImplicitly]
    public int BatteryLevelFull { get; init; } = 100;
}
