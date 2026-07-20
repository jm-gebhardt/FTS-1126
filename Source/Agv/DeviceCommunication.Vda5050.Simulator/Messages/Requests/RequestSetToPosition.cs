using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Requests;

/// <summary>
/// Provides a request to move an agv to a new position.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="Position">Thew new position.</param>
public record RequestSetToPosition(Guid CorrelationId, AgvPosition Position) : CorrelatedBy<Guid>;