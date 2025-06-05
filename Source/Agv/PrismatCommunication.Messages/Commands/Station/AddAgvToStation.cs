using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Messages.Commands.Station;

/// <summary>
/// Provides a command to add an agv to a station.
/// </summary>
/// <param name="CorrelationId">The correlation id of the station.</param>
/// <param name="AgvCorrelationId">The correlation id of the agv.</param>
public record AddAgvToStation(Guid CorrelationId, Guid AgvCorrelationId) : CorrelatedBy<Guid>;