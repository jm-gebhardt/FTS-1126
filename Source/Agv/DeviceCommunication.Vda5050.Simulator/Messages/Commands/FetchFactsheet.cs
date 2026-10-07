using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;

/// <summary>
/// Represents a command to fetch the factsheet of an agv type.
/// </summary>
/// <param name="CorrelationId">The id of the agv type.</param>
/// <param name="AgvTypeIdentifier">The identifier of the agv type.</param>
public record FetchFactsheet(Guid CorrelationId, string AgvTypeIdentifier) : CorrelatedBy<Guid>;