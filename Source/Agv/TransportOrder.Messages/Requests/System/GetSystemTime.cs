using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.System;

/// <summary>
/// Represents the request which returns the system time.
/// </summary>
public record GetSystemTime(Guid CorrelationId) : CorrelatedBy<Guid>;