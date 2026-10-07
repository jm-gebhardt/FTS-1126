using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;

/// <summary>
/// Represents a command to cache the factsheet of an agv type.
/// </summary>
/// <param name="CorrelationId">The id of the agv type.</param>
/// <param name="AgvTypeIdentifier">The identifier of the agv type.</param>
/// <param name="FactsheetJson">The factsheet as raw JSON.</param>
public record CacheFactsheet(Guid CorrelationId, string AgvTypeIdentifier, string FactsheetJson) : CorrelatedBy<Guid>;