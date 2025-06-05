using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Events;

/// <summary>
/// Represents an event that is raised when the request to resolve agv resource errors is received.
/// </summary>
/// <param name="CorrelationId">The agv that left the station.</param>
public record SolveResourceErrorsReceived(Guid CorrelationId): CorrelatedBy<Guid>;