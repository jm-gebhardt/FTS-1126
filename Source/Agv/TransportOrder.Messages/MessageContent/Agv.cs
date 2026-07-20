using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.Contracts;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.MessageContent;

/// <summary>
/// Represents a message content for an agv.
/// </summary>
[PublicAPI]
public record Agv : IAgv
{
    /// <summary>Is agv disturbed. There is at least one error with level "FATAL"</summary>
    public bool IsDisturbed { get; init; }

    /// <summary>AGV is in automatic mode</summary>
    public bool IsAutomatic { get; init; }

    /// <summary>
    /// The agv operating mode.
    /// </summary>
    public AgvOperatingMode OperatingMode { get; init; }

    /// <inheritdoc />    
    public Guid CorrelationId { get; init; }

    /// <inheritdoc cref="IAgv.CorrelationId" />
    public string SerialNumber { get; init; } = null!;

    /// <summary>The name of the agv.</summary>
    public string Name { get; init; } = null!;

    /// <summary>Current State</summary>
    public AgvState CurrentState { get; init; }
   
    /// <summary>Id of agv type.</summary>
    public Guid? AgvTypeId { get; init; }

    /// <summary>Time of the last state transition</summary>
    public DateTime TimeOfStateTransition { get; init; }

    /// <summary>Time of the last lock state change</summary>
    public DateTime TimeOfLockStateChanged { get; init; }

    /// <summary>Communication alive state</summary>
    public bool IsOnline { get; init; }

    /// <summary>Locked state</summary>
    public bool IsLocked { get; init; }

    /// <summary>Battery charge level (Soc)</summary>
    public double BatteryCharge { get; init; }

    /// <summary>Is Battery currently charging</summary>
    public bool IsCharging { get; init; }

    /// <summary>
    /// The recent ActionGroup of the Agv
    /// </summary>
    public ActionGroupReadModel? CurrentActionGroup { get; init; }

    /// <summary>
    /// List of Agv errors. Empty List if there is no error.
    /// </summary>
    public List<AgvError> Errors { get; init; } = new();

    /// <summary>The last node of the agv.</summary>
    public GraphNode? LastNode { get; init; }

    /// <summary>
    /// The agv mode.
    /// </summary>
    public AgvMode AgvMode { get; init; } = null!;

    /// <summary>
    /// The zones that this agv is in.
    /// </summary>
    public List<string> Zones { get; init; } = [];

    /// <summary>
    /// The stations that the agv reserved device-internally.
    /// </summary>
    public List<string> Stations { get; init; } = [];

    /// <summary>Total distance driven by AGV since commissioning [km].</summary>
    public float TotalDistance { get; init; }

    /// <summary>Total lifetime driven by AGV since commissioning [h].</summary>
    public float TotalLifetime { get; init; }

    /// <summary>
    /// True, if the type change has been confirmed. Only applicable if it needs confirmation.
    /// </summary>
    public bool? TypeChangeConfirmed { get; init; }

    /// <summary>
    /// The last maintenance of the agv.
    /// </summary>
    public Maintenance? LastMaintenance { get; init; }

    /// <param name="correlationId">CorrelationId</param>
    /// <param name="serialNumber">The serial number of the agv.</param>
    /// <param name="name">The name of the agv.</param>
    /// <param name="currentState">Current State</param>
    /// <param name="timeOfStateTransition">Time of the last state transition</param>
    /// <param name ="timeOfLockStateChanged">Time of the last lock state change</param>
    /// <param name="isOnline">Communication alive state</param>
    /// <param name="isLocked">Locked state</param>
    /// <param name="isDisturbed">Is agv disturbed. There is at least one error with level "FATAL"</param>
    /// <param name="isAutomatic">AGV is in automatic mode</param>
    /// <param name="batteryCharge">Battery charge level (Soc)</param>
    /// <param name="isCharging">Is Battery currently charging</param>
    /// <param name="currentActionGroup">The recent ActionGroup of the Agv</param>
    /// <param name="agvError">List of Agv errors</param>
    /// <param name="lastNode">The last node of the agv.</param>
    /// <param name="mode">The agv mode.</param>
    /// <param name="zones">the zones that this agv is in.</param>
    /// <param name="stations">the zones that this agv is in.</param>
    /// <param name="totalDistance">Total distance driven by AGV since commissioning [km].</param>
    /// <param name="totalLifetime">Total lifetime driven by AGV since commissioning [h].</param>
    /// <param name="lastMaintenance">The last maintenance of the agv.</param>
    /// <param name="agvTypeId">The id of the agv type.</param>
    /// <param name="typeChangeConfirmed">The type change confirmation state.</param>
    /// <param name="operatingMode">The operating mode.</param>
    [PublicAPI]
    public Agv(
        Guid correlationId,
        string serialNumber,
        string name,
        AgvState currentState,
        DateTime timeOfStateTransition,
        DateTime timeOfLockStateChanged,
        bool isOnline,
        bool isLocked,
        bool isDisturbed,
        bool isAutomatic,
        double batteryCharge,
        bool isCharging,
        ActionGroupReadModel? currentActionGroup,
        List<AgvError> agvError,
        GraphNode? lastNode,
        AgvMode mode,
        List<string> zones,
        List<string> stations,
        float totalDistance,
        float totalLifetime,
        Maintenance? lastMaintenance = null,
        Guid? agvTypeId = null,
        bool? typeChangeConfirmed = null,
        AgvOperatingMode operatingMode = AgvOperatingMode.Automatic
    )
    {
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
        Name = name;
        CurrentState = currentState;
        TimeOfStateTransition = timeOfStateTransition;
        TimeOfLockStateChanged = timeOfLockStateChanged;
        IsOnline = isOnline;
        IsLocked = isLocked;
        IsDisturbed = isDisturbed;
        IsAutomatic = isAutomatic;
        BatteryCharge = batteryCharge;
        IsCharging = isCharging;
        CurrentActionGroup = currentActionGroup;
        Errors = agvError;
        LastNode = lastNode;
        AgvMode = mode;
        Zones = zones;
        Stations = stations;
        TotalDistance = totalDistance;
        TotalLifetime = totalLifetime;
        LastMaintenance = lastMaintenance;
        AgvTypeId = agvTypeId;
        TypeChangeConfirmed = typeChangeConfirmed;
        OperatingMode = operatingMode;
    }

    /// <summary>
    /// AutoMapper ctor
    /// </summary>
    public Agv()
    {
    }
}