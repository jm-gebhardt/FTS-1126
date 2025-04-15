namespace Gebhardt.Acc.KaeserSidecar.Messages;

/// <summary>
/// Provides a command to occupy a station.
/// </summary>
/// <param name="Station">The station name.</param>
/// <param name="AgvCorrelationId">The correlation id of the agv.</param>
public record OccupyStation(string Station, Guid AgvCorrelationId);