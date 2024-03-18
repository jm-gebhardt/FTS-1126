using Gebhardt.StoreWare.Agv.Common.WebApi;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Dto for battery status of an AGV.
/// </summary>
public record AgvBatteryStatusDto : DatedMessage
{
    /// <summary>The CorrelationId of the Agv</summary>
    public Guid CorrelationId { get; init; }

    /// <summary>The current SOC [%]</summary>
    public double BatteryCharge { get; init; }

    /// <summary>Is Battery currently charging.</summary>
    public bool IsCharging { get; init; }

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

    /// <inheritdoc cref="AgvBatteryStatusDto"/>
    /// <param name="correlationId">The CorrelationId of the Agv</param>
    /// <param name="batteryCharge">The current SOC [%]</param>
    /// <param name="isCharging">Is battery currently charging?</param>
    /// <param name="batteryVoltage">Battery voltage [V].</param>
    /// <param name="batteryCurrent">Battery current [A].</param>
    /// <param name="batteryTemperature">Battery temperature [°C].</param>
    /// <param name="estimatedFullyChargedTime">Time when the Agv is fully charged.</param>
    /// <param name="lastBatteryReplacement">The date when the battery was replaced the last time.</param>
    /// <param name="batteryLevelCritical">Below this threshold only charge actions will be started.</param>
    /// <param name="batteryLevelLow">Below this threshold only ActionGroups are started when the system is under high load.</param>
    /// <param name="minChargeUp">Once charging is started it will at least charge up to this level.</param>
    /// <param name="maxChargeUp">We stop charging at this level actively.</param>
    public AgvBatteryStatusDto(
        [PublicAPI] Guid correlationId,
        double batteryCharge,
        bool isCharging,
        double batteryVoltage,
        double batteryCurrent,
        double batteryTemperature,
        DateTime? estimatedFullyChargedTime,
        DateTime? lastBatteryReplacement,
        int batteryLevelCritical,
        int batteryLevelLow,
        int minChargeUp,
        int maxChargeUp
    )
    {
        CorrelationId = correlationId;
        IsCharging = isCharging;
        BatteryCharge = batteryCharge;
        BatteryVoltage = batteryVoltage;
        BatteryCurrent = batteryCurrent;
        BatteryTemperature = batteryTemperature;
        EstimatedFullyChargedTime = estimatedFullyChargedTime;
        LastBatteryReplacement = lastBatteryReplacement;
        BatteryLevelCritical = batteryLevelCritical;
        BatteryLevelLow = batteryLevelLow;
        MinChargeUp = minChargeUp;
        MaxChargeUp = maxChargeUp;
    }

    /// <summary>
    /// ctor called by AutoMapper.
    /// </summary>
    public AgvBatteryStatusDto()
    {
    }
}