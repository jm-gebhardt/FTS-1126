using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Station;

/// <summary>
/// A station
/// </summary>
[PublicAPI]
public class Station
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
    public double Length { get; private set; }

    /// <summary>
    /// Width of the station [m].
    /// </summary>
    public double Width { get; private set; }

    /// <summary>
    /// The primary type of the station.
    /// </summary>
    public AgvGraph.NodeType PrimaryType { get; set; }

    /// <summary>
    /// Additional station types.
    /// </summary>
    public ICollection<AgvGraph.NodeType> AdditionalTypes { get; set; } = [];

    /// <summary>
    /// The agvs currently heading towards this node.
    /// </summary>
    public ICollection<Guid> Agvs { get; set; } = [];

    /// <summary>
    /// Reference to the corresponding node lock.
    /// </summary>
    public NodeLock? NodeLock { get; set; }
   
    /// <summary>
    /// The user that locked the station.
    /// </summary>

    public string? LockedBy { get; set; }

    /// <summary>
    /// The reason the station was locked. For internal logging only.
    /// </summary>
    public string? LockReason { get; set; }

    /// <summary>
    /// Description of the lock.
    /// </summary>
    public string? LockDescription { get; set; }

    /// <summary>
    /// Defines how already started orders will be handled that involve a locked station.
    /// </summary>
    public LockedOrderHandlingType? LockedOrderHandling { get; set; }

    /// <inheritdoc cref="Station"/>>
    public Station(string name, AgvGraph.NodeType primaryType, ICollection<AgvGraph.NodeType> additionalTypes)
    {
        Name = name;
        Id = name.ToGuid();
        PrimaryType = primaryType;
        AdditionalTypes = additionalTypes;
    }

    /// <inheritdoc cref="Station"/>>
    public Station()
    {
        Name = string.Empty;
        MapName = string.Empty;
        Id = Guid.Empty;
    }

    /// <summary>
    /// Set the size of a station
    /// </summary>
    /// <param name="width">The width of the station.</param>
    /// <param name="length">The length of the station.</param>
    public void SetSize(double width, double length)
    {
        Width = width;
        Length = length;
    }

    /// <summary>
    /// Set the map name.
    /// </summary>
    /// <param name="mapName"></param>
    public void SetMap(string? mapName)
    {
        MapName = mapName;
    }

    /// <summary>
    /// Update the state of a station
    /// </summary>
    /// <param name="user">The user that locked the station.</param>
    /// <param name="reason">The reason for locking the station.</param>
    /// <param name="lockDescription">The description of the lock.</param>
    /// <param name="lockedOrderHandling">How orders concerning this station should be handled.</param>
    public void UpdateLockState(string? user, string? reason, string? lockDescription, LockedOrderHandlingType? lockedOrderHandling)
    {
        LockedBy = user;
        LockReason = reason;
        LockDescription = lockDescription;
        LockedOrderHandling = lockedOrderHandling;
    }
}

/// <summary>
/// Defines how orders involving a locked station will be handled that have already started.
/// </summary>
public enum LockedOrderHandlingType
{
    /// <summary>
    /// Abort the orders.
    /// </summary>
    Abort,

    /// <summary>
    /// Pause the orders.
    /// </summary>
    OnHold,

    /// <summary>
    /// Allow the orders to finish.
    /// </summary>
    AllowFinish
}