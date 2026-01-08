using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.StationCommunication.Messages.JamState;

/// <summary>
/// Provides a request to get the current jam state of a station.
/// </summary>
/// <param name="Station">The station.</param>
[PublicAPI]
public record RequestJamState(string Station) : CorrelatedBy<string>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public string CorrelationId  => Station;
}