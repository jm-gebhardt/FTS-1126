using MassTransit;

namespace Gebhardt.Acc.StationCommunication.Messages.JamState;

/// <summary>
/// Provides a command to update a jam state.
/// </summary>
/// <param name="StationId">The station Id.</param>
/// <param name="Status">The status.</param>
/// <param name="TimeStamp">The time stamp of receiving the state.</param>
public record UpdateJamState(Guid StationId, int Status, DateTime TimeStamp) : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public Guid CorrelationId => StationId;
}