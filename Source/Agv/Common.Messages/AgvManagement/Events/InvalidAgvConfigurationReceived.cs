using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Events;

/// <summary>
/// Represents the event raised when an invalid agv configuration has been received.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="SerialNumber">The serial number of the agv.</param>
public record InvalidAgvConfigurationReceived(Guid CorrelationId, string SerialNumber) : CorrelatedBy<Guid>;