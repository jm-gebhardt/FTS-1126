using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.System;

/// <summary>
/// Represents the request which returns the system time.
/// </summary>
public record GetSystemTime(Guid CorrelationId) : CorrelatedBy<Guid>;