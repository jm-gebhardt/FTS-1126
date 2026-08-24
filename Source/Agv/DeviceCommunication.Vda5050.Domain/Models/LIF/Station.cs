using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Helper;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

/// <summary>
/// A station.
/// </summary>
[PublicAPI]
public class Station
{
    /// <inheritdoc cref="Station"/>
    public Station(
        Guid fleetManagerId,
        string stationId,
        string[] interactionNodeIds,
        Guid[] fleetManagerNodeIds,
        string? stationDescription = null,
        double? stationHeight = null,
        string? stationName = null,
        StationPosition? stationPosition = null)
    {
        FleetManagerId = fleetManagerId;
        StationId = stationId;
        FleetManagerNodeIds = fleetManagerNodeIds;
        InteractionNodeIds = interactionNodeIds;
        StationDescription = stationDescription;
        StationHeight = stationHeight;
        StationName = stationName;
        StationPosition = stationPosition;
    }

    /// <inheritdoc cref="Station"/>
    public Station()
    {
    }

    /// <summary>
    /// The edge id used in the fleet manager.
    /// </summary>
    public Guid FleetManagerId { get; set; } = Guid.Empty;

    /// <summary>
    /// List of node IDs where the station interacts.
    /// </summary>
    public string[] InteractionNodeIds { get; set; } = [];

    /// <summary>
    /// List of node IDs where the station interacts.
    /// </summary>
    public Guid[] FleetManagerNodeIds { get; set; } = [];

    /// <summary>
    /// Description of the station. Optional.
    /// </summary>
    public string? StationDescription { get; set; }

    /// <summary>
    /// Height of the station, if applicable, in meters. Optional. Range: [0.0 ... float64.max]
    /// </summary>
    public double? StationHeight { get; set; }

    /// <summary>
    /// Unique identifier for the station.
    /// </summary>
    public string StationId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the station. Optional.
    /// </summary>
    public string? StationName { get; set; }

    /// <summary>
    /// Position of the station on the map (in meters). Optional.
    /// </summary>
    public StationPosition? StationPosition { get; set; }

    /// <summary>
    /// Converts this station into a <see cref="Common.Domain.Models.NewGraph.Station"/>.
    /// </summary>
    /// <param name="stationId">The id of the station.</param>
    /// <param name="layoutId">The layout id if any.</param>
    /// <param name="projectIdentification">The project identifier.</param>
    /// <param name="nodes">The nodes of the layout.</param>
    public Common.Domain.Models.NewGraph.Station ToGraphStation(Guid stationId, string layoutId, string projectIdentification, Dictionary<Guid, Common.Domain.Models.NewGraph.Node> nodes)
    {
        if (StationPosition == null)
        {
            throw new ArgumentException($"Station {layoutId} - {StationName} has no position");
        }

        List<Common.Domain.Models.NewGraph.Node> interactionNodes = InteractionNodeIds.Select(i => nodes[LifHelper.GenerateInternalId(i, layoutId, projectIdentification)]).ToList();

        return new Common.Domain.Models.NewGraph.Station(stationId, StationName, StationDescription, StationHeight, StationPosition.X, StationPosition.Y, StationPosition.Theta)
        {
            InteractionNodes = interactionNodes
        };
    }
}
