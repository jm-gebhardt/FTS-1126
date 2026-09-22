using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models.NewGraph;

/// <summary>
/// Represents a station in the system. A station is a specific location where AGVs can interact with the environment, such as loading or unloading goods.
/// </summary>
[PublicAPI]
public record Station : GraphResource
{
    private IReadOnlyList<Node> _interactionNodes = new List<Node>();
    private IReadOnlyList<Guid>? _interactionNodeIds;

    /// <summary>
    /// X position of the station on the layout in reference to the global origin.
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// Y position of the station on the layout in reference to the global origin.
    /// </summary>
    public double Y { get; init; }

    /// <summary>
    /// Absolute orientation of the station in radians.
    /// </summary>
    public double? Theta { get; init; }

    /// <summary>
    /// These are Ids of the nodes that represent the position at which interaction with this station takes place. Used for serialization and deserialization purposes.
    /// </summary>
    public IReadOnlyList<Guid> InteractionNodeIds
    {
        get => _interactionNodeIds ?? InteractionNodes.Select(n => n.Id).ToList();
        init => _interactionNodeIds = value;
    }

    /// <summary>
    /// The nodes that represent the position at which interaction with this station takes place.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<Node> InteractionNodes
    {
        get => _interactionNodes;
        init => _interactionNodes = value;
    }

    /// <summary>
    /// Absolute physical height of the station in meters.
    /// </summary>
    public double? StationHeight { get; init; }

    /// <inheritdoc cref="Station"/>
    public Station(Guid id, string name, string shortName, double x, double y, double? theta, double? stationHeight, List<Guid> interactionNodeIds) : base(id, name, shortName)
    {
        StationHeight = stationHeight;
        InteractionNodeIds = interactionNodeIds;
        X = x;
        Y = y;
        Theta = theta;
    }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    public Station()
    {
    }

    /// <summary>
    /// Updates the interaction nodes of the station and their corresponding Ids.
    /// </summary>
    public void UpdateInteractionNodes(IEnumerable<Node> interactionNodes)
    {
        _interactionNodes = interactionNodes.ToList();
        _interactionNodeIds = _interactionNodes.Select(n => n.Id).ToList();
    }
}