using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;

/// <summary>
/// Represents a command to publish the factsheet of an agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="ActionId">The id of the factsheetRequest action.</param>
public record RequestFactsheet(Guid CorrelationId, string ActionId) : CorrelatedBy<Guid>;