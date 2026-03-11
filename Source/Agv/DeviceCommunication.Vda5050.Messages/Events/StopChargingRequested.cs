using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;

/// <summary>
/// Represents the event raised when the fleet manager requests to stop charging. 
/// </summary>
/// <param name="CorrelationId"></param>
public record StopChargingRequested(Guid CorrelationId) : CorrelatedBy<Guid>;