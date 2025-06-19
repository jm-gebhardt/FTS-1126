using MassTransit;

namespace Gebhardt.Acc.KaeserSidecar.Messages.JamState;

/// <summary>
/// Provides a command to update a jam state.
/// </summary>
/// <param name="Station">The station.</param>
/// <param name="Status">The status.</param>
/// <param name="TimeStamp">The time stamp of receiving the state.</param>
public record UpdateJamState(string Station, int Status, DateTime TimeStamp) : CorrelatedBy<string>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public string CorrelationId => Station;
}