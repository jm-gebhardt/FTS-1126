using MassTransit;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Commands;

/// <summary>
/// Represents a command to resend an action to an agv.
/// </summary>
/// <param name="CorrelationId">Correlation id of the agv.</param>
/// <param name="SerialNumber">The serial number of the agv.</param>
/// <param name="Action">The action.</param>
public record ResendAction(Guid CorrelationId, string SerialNumber, AgvAction Action): CorrelatedBy<Guid>;