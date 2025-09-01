using System.Text.Json.Serialization;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.Acc.Common.Domain.Models;

/// <summary>
/// AGV Status
/// </summary>
public record AgvStatus : CorrelatedBy<Guid>
{
    /// <summary>
    /// AGV correlation id.
    /// </summary>
    [UsedImplicitly]
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Current or previous order ID (Internally we are using the ActionCorrelationId).
    /// </summary>
    /// <remarks>
    /// VDA5050.state.schema.orderId
    /// The order ID is kept until a new order is received.
    /// </remarks>
    [UsedImplicitly]
    public string? OrderId { get; init; }

    /// <summary>
    /// AGV is busy (not idle).
    /// </summary>
    /// <remarks>
    /// VDA5050.state.schema.actionStates.actionStatus 
    /// There is at least one action pending (neither finished no failed).
    /// </remarks>
    [UsedImplicitly]
    public bool Busy { get; init; }

    /// <summary>
    /// AGV is in automatic mode.
    /// </summary>
    /// <remarks>
    /// VDA5050.state.schema.operatingMode 
    /// Only mode "AUTOMATIC" is regarded.
    /// </remarks>
    [UsedImplicitly]
    public bool Automatic { get; init; }

    /// <summary>
    /// Battery state of charge [%].
    /// </summary>
    /// <remarks>
    /// VDA5050.state.schema.batteryState.batteryCharge
    /// Battery charge level in [%].
    /// </remarks>
    [UsedImplicitly]
    public double BatteryCharge { get; init; }

    /// <summary>
    /// AGV is charging.
    /// </summary>
    /// <remarks>
    /// VDA5050.state.schema.batteryState.charging
    /// Charging in progress.
    /// </remarks>
    [UsedImplicitly]
    public bool Charging { get; init; }

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

    /// <summary>
    /// AGV is disturbed.
    /// </summary>
    /// <remarks>
    /// VDA5050.state.schema.errors.errorLevel
    /// There is at least one error with level "FATAL".
    /// </remarks>
    [UsedImplicitly]
    public bool Disturbed { get; init; }

    /// <summary>
    /// Node id of last reached node or, if AGV is currently on a node, current node.
    /// Empty string if no lastNodeId is available.
    /// </summary>
    /// <remarks>
    /// VDA5050.state.schema.lastNodeId
    /// </remarks>
    [UsedImplicitly]
    public string? LastNodeId { get; init; }

    /// <summary>
    /// The zones that the agv is in, if any.
    /// </summary>
    public ICollection<string>? ZoneIds { get; }

    /// <summary>
    /// The stations that the agv reserved device-internally, if any.
    /// </summary>
    public ICollection<string>? StationIds { get; }

    /// <summary>Total distance since commissioning [km].</summary>
    [UsedImplicitly]
    public float TotalDistance { get; init; }

    /// <summary>Total lifetime since commissioning [h].</summary>
    [UsedImplicitly]
    public float TotalLifetime { get; init; }

    /// <inheritdoc cref="AgvStatus"/>
    /// <param name="correlationId">AGV correlation id.</param>
    /// <param name="orderId">Internally we are using the ActionCorrelationId</param>
    /// <param name="busy">AGV is busy (not idle).</param>
    /// <param name="automatic">AGV is in automatic mode.</param>
    /// <param name="batteryCharge">Battery state of charge [%].</param>
    /// <param name="charging">AGV is charging.</param>
    /// <param name="batteryVoltage">Battery voltage [V].</param>
    /// <param name="batteryCurrent">Battery current [A].</param>
    /// <param name="batteryTemperature">Battery temperature [°C].</param>
    /// <param name="disturbed">AGV is disturbed.</param>
    /// <param name="lastNodeId">Node id of last reached node or, if AGV is currently on a node, current node. Empty string if no lastNodeId is available.</param>
    /// <param name="estimatedFullyChargedTime">Time when the Agv is fully charged.</param>
    /// <param name="lastBatteryReplacement">The date when the battery was replaced the last time.</param>
    /// <param name="zoneIds">The ids of the zones the agv is in, if any.</param>
    /// <param name="stationIds">The ids of the station the agv reserved, if any.</param>
    /// <param name="totalDistance">Total distance driven by AGV since commissioning [km].</param>
    /// <param name="totalLifetime">Total lifetime driven by AGV since commissioning [h].</param>
    [JsonConstructor]
    public AgvStatus(Guid correlationId,
        string? orderId,
        bool busy,
        bool automatic,
        double batteryCharge,
        bool charging,
        double batteryVoltage,
        double batteryCurrent,
        double batteryTemperature,
        bool disturbed,
        string? lastNodeId = null,
        DateTime? estimatedFullyChargedTime = null,
        DateTime? lastBatteryReplacement = null,
        ICollection<string>? zoneIds = null,
        ICollection<string>? stationIds = null,
        float totalDistance = 0,
        float totalLifetime = 0
    )
    {
        CorrelationId = correlationId;
        OrderId = orderId;
        Busy = busy;
        Automatic = automatic;
        BatteryCharge = batteryCharge;
        Charging = charging;
        BatteryVoltage = batteryVoltage;
        BatteryCurrent = batteryCurrent;
        BatteryTemperature = batteryTemperature;
        Disturbed = disturbed;
        LastNodeId = lastNodeId;
        EstimatedFullyChargedTime = estimatedFullyChargedTime;
        LastBatteryReplacement = lastBatteryReplacement;
        ZoneIds = zoneIds;
        StationIds = stationIds;
        TotalDistance = totalDistance;
        TotalLifetime = totalLifetime;
    }

    /// <summary>
    /// For mocking
    /// </summary>
    public AgvStatus()
    {
        CorrelationId = Guid.Empty;
        OrderId = null;
        Busy = false;
        Automatic = false;
        BatteryCharge = 0;
        Charging = false;
        Disturbed = false;
        LastNodeId = null;
        TotalDistance = 0;
        TotalLifetime = 0;
    }
}