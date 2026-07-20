using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvActions;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// AGV model dto.
/// </summary>
[PublicAPI]
public record AgvDto : DatedMessage
{
    /// <summary>Is agv disturbed. There is at least one error with level "FATAL".</summary>
    public bool IsDisturbed { get; init; }

    /// <summary>Agv is in automatic mode.</summary>
    public bool IsAutomatic { get; init; }

    /// <summary>
    /// The current agv operating mode.
    /// </summary>
    public AgvOperatingMode OperatingMode { get; init; }

    /// <summary>The CorrelationId of the agv.</summary>
    public Guid CorrelationId { get; init; }

    /// <summary>Id of agv type.</summary>
    public Guid? AgvTypeId { get; init; }

    /// <summary>The serial number of the agv.</summary>
    public string SerialNumber { get; init; }

    /// <summary>The name of the agv.</summary>
    public string Name { get; init; }

    /// <summary>The current state of the agv.</summary>
    public AgvStateType CurrentState { get; init; }

    /// <summary>Time of the last state transition.</summary>
    public DateTime TimeOfStateTransition { get; init; }

    /// <summary>Time of the last lock state change.</summary>
    public DateTime TimeOfLockStateChanged { get; init; }

    /// <summary>Communication alive state.</summary>
    public bool IsOnline { get; init; }

    /// <summary>Locked state.</summary>
    public bool IsLocked { get; init; }

    /// <summary>Battery charge level (soc).</summary>
    public double BatteryCharge { get; init; }

    /// <summary>Is Battery currently charging.</summary>
    public bool IsCharging { get; init; }

    /// <summary>
    /// The recent action group of the agv.
    /// </summary>
    public ActionGroupDto? CurrentActionGroup { get; init; }

    /// <summary>
    /// List of agv errors. Empty list if there is no error.
    /// </summary>
    public List<AgvErrorDto> Errors { get; set; } = new();

    /// <summary>The last node id for the agv.</summary>
    public GraphNodeDto? LastNode { get; init; }

    /// <summary>
    /// List of the zones that the agv is in.
    /// </summary>
    public List<string> Zones { get; init; }

    /// <summary>
    /// List of the stations that the agv reserved device-internally.
    /// </summary>
    public List<string> Stations { get; init; }

    /// <summary>
    /// The mode of the agv.
    /// </summary>
    public AgvModeDto AgvMode { get; init; }

    /// <summary>Total distance driven by AGV since commissioning [km].</summary>
    [UsedImplicitly]
    public float TotalDistance { get; init; }

    /// <summary>Total lifetime driven by AGV since commissioning [h].</summary>
    [UsedImplicitly]
    public float TotalLifetime { get; init; }

    /// <summary>
    /// True, if the type change has been confirmed. Only applicable if it needs confirmation.
    /// </summary>
    public bool? TypeChangeConfirmed { get; init; }

    /// <summary>
    /// The last maintenance of the agv.
    /// </summary>
    public MaintenanceDto? LastMaintenance { get; init; }

    /// <param name="correlationId">CorrelationId.</param>
    /// <param name="serialNumber">AGV Serial Number.</param>
    /// <param name="name">The name of the agv.</param>
    /// <param name="currentState">Current State.</param>
    /// <param name="timeOfStateTransition">Time of the last state transition.</param>
    /// <param name ="timeOfLockStateChanged">Time of the last lock state change.</param>
    /// <param name="isOnline">Communication alive state.</param>
    /// <param name="isLocked">Locked state.</param>
    /// <param name="isDisturbed">Is agv disturbed. There is at least one error with level "FATAL".</param>
    /// <param name="isAutomatic">Agv is in automatic mode.</param>
    /// <param name="batteryCharge">Battery charge level (soc).</param>
    /// <param name="isCharging">Is battery currently charging?</param>
    /// <param name="currentActionGroup">The recent action group of the agv.</param>
    /// <param name="agvErrorDto">List of agv errors.</param>
    /// <param name="lastNode">Last node.</param>
    /// <param name="mode">The agv mode.</param>
    /// <param name="zones">The zones that the agv is in .</param>
    /// <param name="stations">The stations that the agv reserved.</param>
    /// <param name="totalDistance">Total distance driven by AGV since commissioning [km].</param>
    /// <param name="totalLifetime">Total lifetime driven by AGV since commissioning [h].</param>
    /// <param name="lastMaintenance">Last maintenance information.</param>
    /// <param name="agvTypeId">The id of the agv type.</param>
    /// <param name="typeChangeConfirmed">The type change confirmation state.</param>
    [PublicAPI]
    public AgvDto(Guid correlationId,
        string serialNumber,
        string name,
        AgvStateType currentState,
        DateTime timeOfStateTransition,
        DateTime timeOfLockStateChanged,
        bool isOnline,
        bool isLocked,
        bool isDisturbed,
        bool isAutomatic,
        double batteryCharge,
        bool isCharging,
        ActionGroupDto? currentActionGroup,
        List<AgvErrorDto> agvErrorDto,
        GraphNodeDto? lastNode,
        AgvModeDto mode,
        List<string> zones,
        List<string> stations,
        float totalDistance,
        float totalLifetime,
        MaintenanceDto? lastMaintenance,
        Guid? agvTypeId = null,
        bool? typeChangeConfirmed = null)
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
        Errors = agvErrorDto;
        LastNode = lastNode;
        AgvMode = mode;
        Zones = zones;
        Stations = stations;
        TotalDistance = totalDistance;
        TotalLifetime = totalLifetime;
        LastMaintenance = lastMaintenance;
        AgvTypeId = agvTypeId;
        TypeChangeConfirmed = typeChangeConfirmed;
    }

    /// <summary>
    /// ctor called by AutoMapper.
    /// </summary>
    public AgvDto()
    {
        SerialNumber = null!;
        Name = null!;
        AgvMode = null!;
        Zones = [];
        Stations = [];
        TotalDistance = 0;
        TotalLifetime = 0;
    }
}