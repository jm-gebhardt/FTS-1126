using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Graphs.Events;

/// <summary>
/// Represents the event raised when a configuration for a station has been received.
/// </summary>
/// <param name="CorrelationId">The correlation id of the station.</param>
/// <param name="Configuration">The station configuration.</param>
public record StationConfigurationReceived(Guid CorrelationId, StationConfiguration Configuration): CorrelatedBy<Guid>;

/// <summary>
/// The configuration of the station.
/// </summary>
/// <param name="StationGroup">Names of other stations in the same station group.</param>
public record StationConfiguration(ICollection<string> StationGroup);
 

