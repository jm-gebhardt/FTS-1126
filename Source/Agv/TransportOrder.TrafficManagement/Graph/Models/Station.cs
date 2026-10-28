namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;

/// <summary>
/// Represents a station in the system. A station is a specific location where AGVs can interact with the environment, such as loading or unloading goods.
/// </summary>
public class Station : GraphResource
{
    /// <summary>
    /// StationId in the LIF.
    /// </summary>
    public string LifStationId { get; }
    
    /// <summary>
    /// These are the nodes that represent the position at which interaction with this station takes place.
    /// </summary>
    public IReadOnlyCollection<Node> InteractionNodes { get; }

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
    public Station(Guid id, string name, string shortName, string lifStationId, double? stationHeight, double x, double y, double? theta, List<Node> interactionNodes) : base(id, name, shortName)
    {
        LifStationId = lifStationId;
        StationHeight = stationHeight;
        X = x;
        Y = y;
        Theta = theta;
        InteractionNodes = interactionNodes;
    }

    public override string ToString()
    {
        return $"{nameof(Station)} {Name}, X={X:f3}, Y={Y:f3}, StationHeight={StationHeight:f3}, Theta={Theta:f2}, Id={Id}";
    }
}