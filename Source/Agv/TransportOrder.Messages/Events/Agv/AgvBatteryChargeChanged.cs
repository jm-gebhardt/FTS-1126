using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// An event reporting that the battery charge of an agv has changed.
/// </summary>
public record AgvBatteryChargeChanged : CorrelatedBy<Guid>
{
    /// <summary>The agv correlation id.</summary>
    [UsedImplicitly]
    public Guid CorrelationId { get; init; }

    /// <summary>Battery charge of the agv before the change.</summary>
    [UsedImplicitly]
    public double OldBatteryCharge { get; init; }

    /// <summary>Battery charge of the agv after the change.</summary>
    [UsedImplicitly]
    public double NewBatteryCharge { get; init; }

    /// <summary>Battery voltage [V].</summary>
    [UsedImplicitly]
    public double BatteryVoltage { get; init; }

    /// <summary>Battery current [A].</summary>
    [UsedImplicitly]
    public double BatteryCurrent { get; init; }

    /// <summary>Battery temperature [°C].</summary>
    [UsedImplicitly]
    public double BatteryTemperature { get; init; }

    /// <summary>Time when the Agv is fully charged.</summary>
    [UsedImplicitly]
    public DateTime? EstimatedFullyChargedTime { get; init; }

    /// <summary>The date when the battery was replaced the last time.</summary>
    [UsedImplicitly]
    public DateTime? LastBatteryReplacement { get; init; }

    /// <summary>Below this threshold only charge actions will be started.</summary>
    [UsedImplicitly]
    public int BatteryLevelCritical { get; init; }

    /// <summary>Below this threshold only ActionGroups are started when the system is under high load.</summary>
    [UsedImplicitly]
    public int BatteryLevelLow { get; init; }

    /// <summary>Once charging is started it will at least charge up to this level.</summary>
    [UsedImplicitly]
    public int MinChargeUp { get; init; }

    /// <summary>We stop charging at this level actively.</summary>
    [UsedImplicitly]
    public int MaxChargeUp { get; init; }

    /// <summary>Is Battery currently charging.</summary>
    [UsedImplicitly]
    public bool IsCharging { get; init; }

    /// <summary>
    /// AutoMapper
    /// </summary>
    public AgvBatteryChargeChanged()
    {
    }

    /// <param name="correlationId">The agv correlation id.</param>
    /// <param name="oldBatteryCharge">Battery charge of the agv before the change.</param>
    /// <param name="newBatteryCharge">Battery charge of the agv after the change.</param>
    /// <param name="batteryLevelCritical">Below this threshold only charge actions will be started.</param>
    /// <param name="batteryLevelLow">Below this threshold only ActionGroups are started when the system is under high load.</param>
    /// <param name="minChargeUp">Once charging is started it will at least charge up to this level.</param>
    /// <param name="maxChargeUp">We stop charging at this level actively.</param>
    /// <param name="isCharging">Is battery currently charging?</param>
    /// <param name="batteryVoltage">Battery voltage [V].</param>
    /// <param name="batteryCurrent">Battery current [A].</param>
    /// <param name="batteryTemperature">Battery temperature [°C].</param>
    /// <param name="estimatedFullyChargedTime">Time when the Agv is fully charged.</param>
    /// <param name="lastBatteryReplacement">The date when the battery was replaced the last time.</param>
    public AgvBatteryChargeChanged(Guid correlationId,
        double oldBatteryCharge,
        double newBatteryCharge,
        int batteryLevelCritical,
        int batteryLevelLow,
        int minChargeUp,
        int maxChargeUp,
        bool isCharging,
        double batteryVoltage,
        double batteryCurrent,
        double batteryTemperature,
        DateTime? estimatedFullyChargedTime,
        DateTime? lastBatteryReplacement
    )
    {
        CorrelationId = correlationId;
        OldBatteryCharge = oldBatteryCharge;
        NewBatteryCharge = newBatteryCharge;
        BatteryLevelCritical = batteryLevelCritical;
        BatteryLevelLow = batteryLevelLow;
        MinChargeUp = minChargeUp;
        MaxChargeUp = maxChargeUp;
        IsCharging = isCharging;
        BatteryVoltage = batteryVoltage;
        BatteryCurrent = batteryCurrent;
        BatteryTemperature = batteryTemperature;
        EstimatedFullyChargedTime = estimatedFullyChargedTime;
        LastBatteryReplacement = lastBatteryReplacement;
    }

    /// <param name="correlationId">The agv correlation id.</param>
    /// <param name="oldBatteryCharge">Battery charge of the agv before the change.</param>
    /// <param name="newBatteryCharge">Battery charge of the agv after the change.</param>
    /// <param name="settings">The AgvSettings.</param>
    /// <param name="status">The AgvStatus.</param>
    /// <param name="estimatedFullyChargedTime">Time when the Agv is fully charged.</param>
    /// <param name="lastBatteryReplacement">The date when the battery was replaced the last time.</param>
    public AgvBatteryChargeChanged(Guid correlationId,
        double oldBatteryCharge,
        double newBatteryCharge,
        AgvSettings settings,
        AgvStatus status,
        DateTime? estimatedFullyChargedTime,
        DateTime? lastBatteryReplacement
    ) : this(correlationId, oldBatteryCharge, newBatteryCharge, settings.BatteryLevelCritical, settings.BatteryLevelLow, settings.MinChargeUp, settings.MaxChargeUp, status.Charging, status.BatteryVoltage, status.BatteryCurrent, status.BatteryTemperature, estimatedFullyChargedTime, lastBatteryReplacement)
    {
    }
}