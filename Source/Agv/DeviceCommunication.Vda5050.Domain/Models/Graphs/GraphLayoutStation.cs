using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;

/// <summary>
/// A station within a <see cref="GraphLayout"/>.
/// Stations are interaction points (e.g. pick/drop) linked to graph nodes.
/// </summary>
[PublicAPI]
public record GraphLayoutStation
{
    /// <summary>
    /// The station identifier.
    /// </summary>
    public string StationId { get; init; } = string.Empty;

    /// <summary>
    /// Optional human-readable station name.
    /// </summary>
    public string? StationName { get; init; }

    /// <summary>
    /// Optional description of the station.
    /// </summary>
    public string? StationDescription { get; init; }

    /// <summary>
    /// X position of the station in meters.
    /// </summary>
    public double? PositionX { get; init; }

    /// <summary>
    /// Y position of the station in meters.
    /// </summary>
    public double? PositionY { get; init; }

    /// <summary>
    /// Orientation of the station in radians.
    /// </summary>
    public double? Theta { get; init; }

    /// <summary>
    /// The node Ids that AGVs interact with at this station.
    /// </summary>
    public ICollection<string> InteractionNodeIds { get; init; } = [];
}