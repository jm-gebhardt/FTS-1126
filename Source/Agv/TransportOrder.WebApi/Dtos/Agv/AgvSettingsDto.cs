// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable RedundantDefaultMemberInitializer

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// The settings for Agv.
/// </summary>
public record AgvSettingsDto
{
    /// <summary>Below this threshold only charge actions will be started.</summary>
    public int? BatteryLevelCritical { get; set; } = null;

    /// <summary>Below this threshold only ActionGroups are started when the system is under high load.</summary>
    public int? BatteryLevelLow { get; set; } = null;

    /// <summary>Once charging is started it will at least charge up to this level.</summary>
    public int? MinChargeUp { get; set; } = null;

    /// <summary>We stop charging at this level actively.</summary>
    public int? MaxChargeUp { get; set; } = null;

    /// <inheritdoc cref="AgvSettingsDto"/>>
    public AgvSettingsDto(int? batteryLevelCritical = null, int? batteryLevelLow = null, int? minChargeUp = null, int? maxChargeUp = null)
    {
        BatteryLevelCritical = batteryLevelCritical;
        BatteryLevelLow = batteryLevelLow;
        MinChargeUp = minChargeUp;
        MaxChargeUp = maxChargeUp;
    }
}