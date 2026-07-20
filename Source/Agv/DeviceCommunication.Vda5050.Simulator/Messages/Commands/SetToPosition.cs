using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;

/// <summary>
/// Provides a command to set an agv to the given position.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="Position">The new position.</param>
public record SetToPosition(Guid CorrelationId, AgvPosition Position) : CorrelatedBy<Guid>;