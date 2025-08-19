using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Stations;

/// <summary>
/// Provides a command to unblock a station.
/// </summary>
/// <param name="Station">The station to unblock.</param>
public record UnblockStation(string Station) : CorrelatedBy<string>
{
    /// <inheritdoc />
    public string CorrelationId => Station;
}