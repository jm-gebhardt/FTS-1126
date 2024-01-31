using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;

/// <summary>
/// AGV Status
/// </summary>
public record AgvStatus
{
    /// <summary>
    /// AGV correlation ID
    /// </summary>
    [UsedImplicitly]
    public Guid CorrelationId { get; }

    /// <summary>
    /// Current or previous order ID (Internally we are using the ActionCorrelationId).
    /// </summary>
    /// <remarks>
    /// VDA5050.state.schema.orderId
    /// The order ID is kept until a new order is received.
    /// </remarks>
    [UsedImplicitly]
    public string? OrderId { get; }

    /// <summary>
    /// AGV is busy (not idle).
    /// </summary>
    /// <remarks>
    /// VDA5050.state.schema.actionStates.actionStatus 
    /// There is at least one action pending (neither finished no failed).
    /// </remarks>
    [UsedImplicitly]
    public bool Busy { get; }

    /// <summary>
    /// AGV is in automatic mode.
    /// </summary>
    /// <remarks>
    /// VDA5050.state.schema.operatingMode 
    /// Only mode "AUTOMATIC" is regarded.
    /// </remarks>
    [UsedImplicitly]
    public bool Automatic { get; }

    /// <summary>
    /// Battery charge.
    /// </summary>
    /// <remarks>
    /// VDA5050.state.schema.batteryState.batteryCharge
    /// Battery charge level in [%].
    /// </remarks>
    [UsedImplicitly]
    public double BatteryCharge { get; }

    /// <summary>
    /// AGV is charging.
    /// </summary>
    /// <remarks>
    /// VDA5050.state.schema.batteryState.charging
    /// Charging in progress.
    /// </remarks>
    [UsedImplicitly]
    public bool Charging { get; }

    /// <summary>Battery voltage [V].</summary>
    [UsedImplicitly]
    public double BatteryVoltage { get; init; }

    /// <summary>Battery current [A].</summary>
    [UsedImplicitly]
    public double BatteryCurrent { get; init; }

    /// <summary>Battery temperature [°C].</summary>
    [UsedImplicitly]
    public double BatteryTemperature { get; init; }

    /// <summary>
    /// AGV is disturbed.
    /// </summary>
    /// <remarks>
    /// VDA5050.state.schema.errors.errorLevel
    /// There is at least one error with level "FATAL".
    /// </remarks>
    [UsedImplicitly]
    public bool Disturbed { get; }

    /// <summary>
    /// nodeID of last reached node or, if AGV is currently on a node, current node.
    /// Empty string if no lastNodeId is available.
    /// </summary>
    /// <remarks>
    /// VDA5050.state.schema.lastNodeId
    /// </remarks>
    public string? LastNodeId { get; }

    /// <summary>
    /// Constructor of AgvStatus
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="orderId">Internally we are using the ActionCorrelationId</param>
    /// <param name="busy"></param>
    /// <param name="automatic"></param>
    /// <param name="batteryCharge"></param>
    /// <param name="batteryVoltage">Battery voltage [V].</param>
    /// <param name="batteryCurrent">Battery current [A].</param>
    /// <param name="batteryTemperature">Battery temperature [°C].</param>
    /// <param name="charging"></param>
    /// <param name="disturbed"></param>
    /// <param name="lastNodeId"></param>
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
        double batteryTemperature = double.MaxValue
    )
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
    }
}