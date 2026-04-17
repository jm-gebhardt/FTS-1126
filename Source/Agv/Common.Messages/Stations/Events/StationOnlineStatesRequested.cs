using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Stations.Events;

/// <summary>
/// Requests all stations to re-publish their current online state.
/// </summary>
[PublicAPI]
public record StationOnlineStatesRequested;