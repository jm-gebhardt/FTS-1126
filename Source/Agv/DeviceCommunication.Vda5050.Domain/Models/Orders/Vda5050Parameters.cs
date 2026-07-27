namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

/// <summary>
/// Parameters for VDA5050 predefined actions.
/// </summary>
public record Vda5050Parameters
{
    /// <summary>
    /// The name of the station.
    /// </summary>
    public string? StationName { get; set; }

    /// <summary>
    /// The parameter stationType informs how the pick operation is handled in detail (e.g., floor location, rack location, passive conveyor, active conveyor, etc.).
    /// </summary>
    public string? StationType { get; set; }

    /// <summary>
    /// AGVs with multiple load handling devices can process multiple pick operations in parallel. In this case, the parameter lhd needs to be present (e.g., LHD1).
    /// </summary>
    public string? LoadHandlingDevice { get; set; }

    /// <summary>
    /// The type of the load being handled.
    /// </summary>
    public string? LoadType { get; set; }

    /// <summary>
    /// The current position of the agv.
    /// </summary>
    public Vda5050Position? Position { get; set; }

    /// <summary>
    /// The id of the map.
    /// </summary>
    public string? MapId { get; set; }

    /// <summary>
    /// The map version.
    /// </summary>
    public string? MapVersion { get; set; }

    /// <summary>
    /// The id of the last node the agv passed.
    /// </summary>
    public string? LastNodeId { get; set; }

    /// <summary>
    /// The type of object to detect.
    /// </summary>
    public string? ObjectType { get; set; }

    /// <summary>
    /// The trigger type.
    /// </summary>
    public string? TriggerType { get; set; }
}