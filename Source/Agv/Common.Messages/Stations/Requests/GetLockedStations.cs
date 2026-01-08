using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Stations.Requests;

/// <summary>
/// Represents a request to get all locked stations.
/// </summary>
[PublicAPI]
public record GetLockedStations;