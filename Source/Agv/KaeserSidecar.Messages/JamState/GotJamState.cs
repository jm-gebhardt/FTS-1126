using JetBrains.Annotations;

namespace Gebhardt.Acc.KaeserSidecar.Messages.JamState;

/// <summary>
/// Represents the response to the <see cref="GetJamState"/> request.
/// </summary>
/// <param name="HasJam">True, if there is a pallet jam at the station.</param>
[PublicAPI]
public record GotJamState(bool HasJam);