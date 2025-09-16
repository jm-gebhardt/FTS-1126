using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Stations;

/// <summary>
/// Provides a command to unblock a station.
/// </summary>
/// <param name="Id">The station to unblock.</param>
/// <param name="Name">The name of the station.</param>
public record UnblockStation(Guid Id, string Name) : CorrelatedBy<Guid>
{
    /// <inheritdoc />
    public Guid CorrelationId => Id;
}