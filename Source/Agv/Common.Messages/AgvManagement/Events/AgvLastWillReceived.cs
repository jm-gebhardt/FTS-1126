using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Events;

/// <summary>
/// Represents the event raised when a last will from an agv has been received.
/// </summary>
/// <param name="CorrelationId"></param>
public record AgvLastWillReceived(Guid CorrelationId):CorrelatedBy<Guid>;