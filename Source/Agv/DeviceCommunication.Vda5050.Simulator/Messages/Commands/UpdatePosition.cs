using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;

/// <summary>
/// Provies a command to update the current agv position.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="Position">The current position.</param>
/// <param name="LastNodeId">The last node id.</param>
public record UpdatePosition(Guid CorrelationId, AgvPosition Position, string? LastNodeId = null) : CorrelatedBy<Guid>;