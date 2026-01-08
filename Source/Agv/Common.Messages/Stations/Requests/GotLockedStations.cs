using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Stations.Requests;

/// <summary>
/// Provides a response for <see cref="GetLockedStations"/>.
/// </summary>
/// <param name="Stations">The locked stations.</param>
[PublicAPI]
public record GotLockedStations(ICollection<string> Stations);