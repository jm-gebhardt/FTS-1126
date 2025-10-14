using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Stations;

/// <summary>
/// Provides a command to unblock a station.
/// </summary>
/// <param name="Id">The station to unblock.</param>
public record UnblockStation(Guid Id) : CorrelatedBy<Guid>
{
    /// <inheritdoc />
    public Guid CorrelationId => Id;
}