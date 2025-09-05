using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Agv;

/// <summary>
/// AGV Status
/// </summary>
[PublicAPI]
public record AgvStatus
{
    /// <summary>
    /// AGV correlation ID
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// Current or previous order ID (Internally we are using the ActionCorrelationId).
    /// </summary>
    /// <remarks>
    /// VDA5050.state.schema.orderId
    /// The order ID is kept until a new order is received.
    /// </remarks>
    public string? OrderId { get; }

    /// <summary>
    /// AGV is busy (not idle).
    /// </summary>
    /// <remarks>
    /// VDA5050.state.schema.actionStates.actionStatus 
    /// There is at least one action pending (neither finished no failed).
    /// </remarks>
    public bool Busy { get; }

    /// <summary>
    /// AGV is in automatic mode.
    /// </summary>
    /// <remarks>
    /// VDA5050.state.schema.operatingMode 
    /// Only mode "AUTOMATIC" is regarded.
    /// </remarks>
    public bool Automatic { get; }

    /// <summary>
    /// Battery charge [%].
    /// </summary>
    /// <remarks>
    /// VDA5050.state.schema.batteryState.batteryCharge
    /// Battery charge level in [%].
    /// </remarks>
    public double BatteryCharge { get; init; }

    /// <summary>
    /// AGV is charging.
    /// </summary>
    /// <remarks>
    /// VDA5050.state.schema.batteryState.charging
    /// The charging is in progress.
    /// </remarks>
    public bool Charging { get; }

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

    /// <summary>
    /// AGV is disturbed.
    /// </summary>
    /// <remarks>
    /// VDA5050.state.schema.errors.errorLevel
    /// There is at least one error with level "FATAL".
    /// </remarks>
    public bool Disturbed { get; }

    /// <summary>
    /// nodeID of last reached node or, if AGV is currently on a node, current node.
    /// Empty string if no lastNodeId is available.
    /// </summary>
    /// <remarks>
    /// VDA5050.state.schema.lastNodeId
    /// </remarks>
    public string? LastNodeId { get; private set; }

    /// <summary>
    /// The zones that the agv is in if any.
    /// </summary>
    public ICollection<string>? ZoneIds { get; private set; }

    /// <summary>
    /// The stations that the agv reserved device-internally, if any.
    /// </summary>
    public ICollection<string>? StationIds { get; private set; }

    /// <summary>Total distance driven by AGV since commissioning [km].</summary>
    public float TotalDistance { get; init; }

    /// <summary>Total lifetime driven by AGV since commissioning [h].</summary>
    public float TotalLifetime { get; init; }

    /// <summary>
    /// Constructor of AgvStatus
    /// </summary>
    /// <param name="correlationId">AGV correlation ID</param>
    /// <param name="orderId">Internally we are using the ActionCorrelationId</param>
    /// <param name="busy">AGV is busy (not idle).</param>
    /// <param name="automatic">AGV is in automatic mode.</param>
    /// <param name="batteryCharge">Battery charge [%]</param>
    /// <param name="charging">AGV is charging.</param>
    /// <param name="disturbed">AGV is disturbed.</param>
    /// <param name="lastNodeId">nodeID of last reached node or, if AGV is currently on a node, current node.</param>
    /// <param name="batteryVoltage">Battery voltage [V].</param>
    /// <param name="batteryCurrent">Battery current [A].</param>
    /// <param name="batteryTemperature">Battery temperature [°C].</param>
    /// <param name="estimatedFullyChargedTime">Time when the Agv is fully charged.</param>
    /// <param name="lastBatteryReplacement">The date when the battery was replaced the last time.</param>
    /// <param name="zoneIds">The ids of the zones the agv is in, if any.</param>
    /// <param name="stationIds">The ids of the stations the agv reserved, if any.</param>
    /// <param name="totalDistance">Total distance driven by AGV since commissioning [km].</param>
    /// <param name="totalLifetime">Total lifetime driven by AGV since commissioning [h].</param>
    [JsonConstructor]
    public AgvStatus(Guid correlationId,
        string? orderId,
        bool busy,
        bool automatic,
        double batteryCharge,
        bool charging,
        bool disturbed,
        string? lastNodeId = null,
        double batteryVoltage = double.MaxValue,
        double batteryCurrent = double.MinValue,
        double batteryTemperature = double.MaxValue,
        DateTime? estimatedFullyChargedTime = null,
        DateTime? lastBatteryReplacement = null,
        ICollection<string>? zoneIds = null,
        ICollection<string>? stationIds = null,
        float totalDistance = 0,
        float totalLifetime = 0
    ) : this()
    {
        CorrelationId = correlationId;
        OrderId = orderId;
        Busy = busy;
        Automatic = automatic;
        BatteryCharge = batteryCharge;
        Charging = charging;
        Disturbed = disturbed;
        LastNodeId = lastNodeId;
        BatteryVoltage = batteryVoltage;
        BatteryCurrent = batteryCurrent;
        BatteryTemperature = batteryTemperature;
        EstimatedFullyChargedTime = estimatedFullyChargedTime;
        // if the value is not set, we take the default value to simulate some data
        if (lastBatteryReplacement != null)
        {
            LastBatteryReplacement = lastBatteryReplacement;
        }
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
        LastBatteryReplacement = DateTime.UtcNow.AddDays(-2);
        TotalDistance = 0;
        TotalLifetime = 0;
    }

    /// <summary>
    /// Update the last node id.
    /// </summary>
    /// <param name="nodeId">The new node id.</param>
    public void UpdateLastNodeId(string? nodeId)
    {
        LastNodeId = nodeId;
    }

    /// <summary>
    /// Reset zone ids.
    /// </summary>
    public void ResetZones()
    {
        ZoneIds = [];
    }

    /// <summary>
    /// Remove zones
    /// </summary>
    public void RemoveZones(ICollection<string> removedZones)
    {
        if (ZoneIds?.Count > 0)
        {
            ZoneIds = ZoneIds.Except(removedZones).ToList();
        }
    }

    /// <summary>
    /// Removes the provided zone.
    /// </summary>
    /// <param name="zoneId">The zone to remove.</param>
    public bool RemoveZone(string zoneId)
    {
        return ZoneIds?.Remove(zoneId) ?? false;
    }
}