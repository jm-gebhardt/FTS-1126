using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Stations;

/// <summary>
/// Provides a command to unblock a station.
/// </summary>
/// <param name="StationId">The station to unblock.</param>
public record UnblockStation(Guid StationId) : CorrelatedBy<Guid>
{
    /// <inheritdoc />
    public Guid CorrelationId => StationId;
}