namespace Gebhardt.Acc.TransportOrder.Messages.MessageContent;

/// <summary>
/// Settings for the AGV.
/// </summary>
public record AgvSettings
{
    /// <summary>The agv correlation id</summary>
    public Guid CorrelationId { get; init; }

    /// <summary>Timeout value [ms] to set AGV offline while it is in idle mode</summary>
    public int AgvOfflineTimeoutMsWhileIdle { get; init; }

    /// <summary>Timeout value [ms] to set AGV offline while it is in busy mode</summary>
    public int AgvOfflineTimeoutMsWhileBusy { get; init; }

    /// <summary>Below this threshold only charge actions will be started.</summary>
    public int BatteryLevelCritical { get; init; }

    /// <summary>Below this threshold only ActionGroups are started when the system is under high load.</summary>
    public int BatteryLevelLow { get; init; }

    /// <summary>Once charging is started it will at least charge up to this level.</summary>
    public int MinChargeUp { get; init; }

    /// <summary>We stop charging at this level actively.</summary>
    public int MaxChargeUp { get; init; }
}