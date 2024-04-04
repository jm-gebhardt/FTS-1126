namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// The settings for Agv.
/// </summary>
public record AgvSettingsResponseDto
{
    /// <summary>The correlationId of the Agv.</summary>
    public Guid CorrelationId { get; init; }

    /// <summary>Below this threshold only charge actions will be started.</summary>
    public int? BatteryLevelCritical { get; init; }

    /// <summary>Below this threshold only ActionGroups are started when the system is under high load.</summary>
    public int? BatteryLevelLow { get; init; }

    /// <summary>Once charging is started it will at least charge up to this level.</summary>
    public int? MinChargeUp { get; init; }

    /// <summary>We stop charging at this level actively.</summary>
    public int? MaxChargeUp { get; init; }
}