using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Commands;

/// <summary>
/// Provides a command to free a station.
/// </summary>
/// <param name="Station">The station name.</param>
/// <param name="AgvCorrelationId">The correlation id of the agv.</param>
public record FreeStation(string Station, Guid AgvCorrelationId) : CorrelatedBy<string>
{
    /// <inheritdoc cref="CorrelatedBy{Key}"/>>
    public string CorrelationId => Station;
}