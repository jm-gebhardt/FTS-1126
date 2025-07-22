using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;

/// <summary>
/// The station dto.
/// </summary>
[PublicAPI]
public record StationDto
{
    /// <summary>
    /// The id of the station.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the station.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The name of the map station is on.
    /// </summary>
    public string? MapName { get; set; }

    /// <summary>
    /// Length of the station [m].
    /// </summary>
    public double Length { get;  set; }

    /// <summary>
    /// Width of the station [m].
    /// </summary>
    public double Width { get;  set; }

    /// <summary>
    /// The primary type of the station.
    /// </summary>
    public AgvGraph.NodeType PrimaryType { get; set; }

    /// <summary>
    /// Additional station types.
    /// </summary>
    [PublicAPI]
    public ICollection<AgvGraph.NodeType> AdditionalTypes { get; set; } = [];

    /// <summary>
    /// The agvs currently heading towards this node.
    /// </summary>
    [PublicAPI]
    public ICollection<Guid> Agvs { get; set; } = [];

    /// <summary>
    /// Node lock state
    /// </summary>
    [PublicAPI]
    public NodeLockState State { get; set; }

    /// <summary>
    /// Boolean to determine if station is locked.
    /// </summary>
    [PublicAPI]
    public bool IsLocked { get; set; }

    /// <summary>
    /// Date the station was last modified.
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    /// The user that locked the station.
    /// </summary>
    public string? LockedBy { get; set; }

    /// <summary>
    /// The reason the station was locked.
    /// </summary>
    public string? LockReason { get; set; }

    /// <summary>
    /// The description of the lock.
    /// </summary>
    public string? LockDescription { get; set; }

    /// <summary>
    /// True, if the station has a pallet jam.
    /// </summary>
    public bool HasPalletJam { get; set; }

    /// <summary>
    /// The handling of orders that involve this station.
    /// </summary>
    public LockedOrderHandlingType? LockedOrderHandling { get; set; }

    /// <summary>
    /// ctor called by AutoMapper.
    /// </summary>
    private StationDto()
    {
        Name = string.Empty;
    }
}