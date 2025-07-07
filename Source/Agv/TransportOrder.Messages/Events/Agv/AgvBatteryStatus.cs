using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Battery status of an AGV.
/// </summary>
[PublicAPI]
public record AgvBatteryStatus
{
    /// <summary>The agv correlation id.</summary>
    public Guid CorrelationId { get; init; }

    /// <summary>Battery charge of the agv after the change.</summary>
    public double BatteryCharge { get; init; }

    /// <summary>Battery voltage [V].</summary>
    public double BatteryVoltage { get; init; }

    /// <summary>Battery current [A].</summary>
    public double BatteryCurrent { get; init; }

    /// <summary>Battery temperature [°C].</summary>
    public double BatteryTemperature { get; init; }

    /// <summary>Time when the Agv is fully charged.</summary>
    public DateTime? EstimatedFullyChargedTime { get; init; }

    /// <summary>The date when the battery was replaced the last time.</summary>
    public DateTime? LastBatteryReplacement { get; init; }

    /// <summary>Below this threshold only charge actions will be started.</summary>
    public int BatteryLevelCritical { get; init; }

    /// <summary>Below this threshold only ActionGroups are started when the system is under high load.</summary>
    public int BatteryLevelLow { get; init; }

    /// <summary>Once charging is started it will at least charge up to this level.</summary>
    public int MinChargeUp { get; init; }

    /// <summary>We stop charging at this level actively.</summary>
    public int MaxChargeUp { get; init; }

    /// <summary>Is Battery currently charging.</summary>
    public bool IsCharging { get; init; }

    /// <summary>Charge infinitely, if no order comes in or charging is aborted by the user.</summary>
    public bool InfiniteCharging { get; init; }

    /// <summary>
    /// AutoMapper
    /// </summary>
    public AgvBatteryStatus()
    {
    }

    /// <param name="correlationId">The agv correlation id.</param>
    /// <param name="batteryCharge">Battery charge [%].</param>
    /// <param name="isCharging">Is battery currently charging?</param>
    /// <param name="batteryVoltage">Battery voltage [V].</param>
    /// <param name="batteryCurrent">Battery current [A].</param>
    /// <param name="batteryTemperature">Battery temperature [°C].</param>
    /// <param name="batteryLevelCritical">Below this threshold only charge actions will be started.</param>
    /// <param name="batteryLevelLow">Below this threshold only ActionGroups are started when the system is under high load.</param>
    /// <param name="minChargeUp">Once charging is started it will at least charge up to this level.</param>
    /// <param name="maxChargeUp">We stop charging at this level actively.</param>
    /// <param name="estimatedFullyChargedTime">Time when the Agv is fully charged.</param>
    /// <param name="lastBatteryReplacement">The date when the battery was replaced the last time.</param>
    /// <param name="infiniteCharging">Flag to switch on/off infinite charging.</param>
    public AgvBatteryStatus(
        Guid correlationId,
        double batteryCharge,
        bool isCharging,
        double batteryVoltage,
        double batteryCurrent,
        double batteryTemperature,
        int batteryLevelCritical,
        int batteryLevelLow,
        int minChargeUp,
        int maxChargeUp,
        DateTime? estimatedFullyChargedTime,
        DateTime? lastBatteryReplacement,
        bool infiniteCharging
    )
    {
        CorrelationId = correlationId;
        BatteryCharge = batteryCharge;
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
        InfiniteCharging = infiniteCharging;
    }

    /// <param name="agvStatus">The AgvStatus.</param>
    /// <param name="agvSettings">The AgvSettingsReadModel.</param>
    public AgvBatteryStatus(
        AgvStatus agvStatus,
        AgvSettings agvSettings
    ) : this(
        agvStatus.CorrelationId,
        agvStatus.BatteryCharge,
        agvStatus.Charging,
        agvStatus.BatteryVoltage,
        agvStatus.BatteryCurrent,
        agvStatus.BatteryTemperature,
        agvSettings.BatteryLevelCritical,
        agvSettings.BatteryLevelLow,
        agvSettings.MinChargeUp,
        agvSettings.MaxChargeUp,
        agvStatus.EstimatedFullyChargedTime,
        agvStatus.LastBatteryReplacement,
        agvSettings.InfiniteCharging
    )
    {
    }
}