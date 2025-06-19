using JetBrains.Annotations;

namespace Gebhardt.Acc.KaeserSidecar.Messages.JamState;

/// <summary>
/// Provides a request to get the current jam state of a station.
/// </summary>
/// <param name="Station">The station.</param>
/// <param name="Timestamp">The time of the request.</param>
/// <param name="JamStateWaitingTimeSec">The waiting time after issuing a request to the plc in seconds.</param>
/// <param name="JamStateOutDatedTimeSec">The time difference after which a jam state is considered as outdated.</param>
[PublicAPI]
public record GetJamState(string Station, DateTime Timestamp, int JamStateWaitingTimeSec=10, int JamStateOutDatedTimeSec=120);