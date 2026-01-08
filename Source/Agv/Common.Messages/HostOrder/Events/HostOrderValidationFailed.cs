using MassTransit;

namespace Gebhardt.Acc.Common.Messages.HostOrder.Events;

/// <summary>
/// Represents the event raised when the host order validation failed.
/// </summary>
/// <param name="CorrelationId"></param>
/// <param name="Error"></param>
public record HostOrderValidationFailed(Guid CorrelationId, ValidationError Error) : CorrelatedBy<Guid>;