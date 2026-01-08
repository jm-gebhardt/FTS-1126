using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Graphs.Events;

/// <summary>
/// Represents the event raised when a station was locked.
/// </summary>
/// <param name="StationNodeId">The node id of the station.</param>
[PublicAPI]
public record StationLocked(string StationNodeId);