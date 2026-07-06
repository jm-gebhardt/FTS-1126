namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

/// <summary>
/// Represents a station in the system. A station is a specific location where AGVs can interact with the environment, such as loading or unloading goods.
/// </summary>
public class Station : GraphResource
{
    /// <summary>
    /// These are the nodes that represent the position at which interaction with this station takes place.
    /// </summary>
    public List<Node> InteractionNodes { get; } = [];

    /// <summary>
    /// Absolute physical height of the station in meters.
    /// </summary>
    public double? StationHeight { get; }

    /// <summary>
    /// X position of the station on the layout in reference to the global origin.
    /// </summary>
    public double X { get; }

    /// <summary>
    /// Y position of the station on the layout in reference to the global origin.
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// Absolute orientation of the station in radians.
    /// </summary>
    public double? Theta { get; }

    /// <inheritdoc cref="Station"/>
    public Station(Guid id, string? name, string? description, double? stationHeight, double x, double y, double? theta) : base(id, name, description)
    {
        StationHeight = stationHeight;
        X = x;
        Y = y;
        Theta = theta;
    }
}