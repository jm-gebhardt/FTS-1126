using CommonStation = Gebhardt.Acc.Common.Domain.Models.NewGraph.Station;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.NewGraph;

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
    // ReSharper disable once MemberCanBePrivate.Global
    public Station(Guid id, string name, string shortName, double? stationHeight, double x, double y, double? theta) : base(id, name, shortName)
    {
        StationHeight = stationHeight;
        X = x;
        Y = y;
        Theta = theta;
    }

    /// <summary>
    /// Generates a station from a <see cref="CommonStation"/>.
    /// </summary>
    /// <param name="commonStation">The <see cref="CommonStation"/>.</param>
    /// <param name="nodes">The nodes of the same graph, keyed by their id.</param>
    public static Station FromCommon(CommonStation commonStation, IReadOnlyDictionary<Guid, Node> nodes)
    {
        var station = new Station(commonStation.Id, commonStation.Name, commonStation.ShortName, commonStation.StationHeight, commonStation.X, commonStation.Y, commonStation.Theta);

        station.InteractionNodes.AddRange(commonStation.InteractionNodeIds.Select(i => nodes[i]));

        return station;
    }
}