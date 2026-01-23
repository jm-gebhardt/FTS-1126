using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Graphs.Events;

/// <summary>
/// Represents the event raised when a station was locked.
/// </summary>
/// <param name="StationId">The Id of the station.</param>
[PublicAPI]
public record StationLocked(string StationId);