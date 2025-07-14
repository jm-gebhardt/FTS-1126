using Gebhardt.Acc.Common.Domain.Models;
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

    /// <summary>Is Battery currently charging.</summary>
    public bool IsCharging { get; init; }

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
    /// <param name="estimatedFullyChargedTime">Time when the Agv is fully charged.</param>
    /// <param name="lastBatteryReplacement">The date when the battery was replaced the last time.</param>
    public AgvBatteryStatus(
        Guid correlationId,
        double batteryCharge,
        bool isCharging,
        double batteryVoltage,
        double batteryCurrent,
        double batteryTemperature,
        DateTime? estimatedFullyChargedTime,
        DateTime? lastBatteryReplacement
    )
    {
        CorrelationId = correlationId;
        BatteryCharge = batteryCharge;
        IsCharging = isCharging;
        BatteryVoltage = batteryVoltage;
        BatteryCurrent = batteryCurrent;
        BatteryTemperature = batteryTemperature;
        EstimatedFullyChargedTime = estimatedFullyChargedTime;
        LastBatteryReplacement = lastBatteryReplacement;
    }

    /// <param name="agvStatus">The AgvStatus.</param>
    public AgvBatteryStatus(AgvStatus agvStatus) : this(
        agvStatus.CorrelationId,
        agvStatus.BatteryCharge,
        agvStatus.Charging,
        agvStatus.BatteryVoltage,
        agvStatus.BatteryCurrent,
        agvStatus.BatteryTemperature,
        agvStatus.EstimatedFullyChargedTime,
        agvStatus.LastBatteryReplacement
    )
    {
    }
}