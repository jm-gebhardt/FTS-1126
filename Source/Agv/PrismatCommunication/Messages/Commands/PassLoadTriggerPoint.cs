using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Messages.Commands;

/// <summary>
/// Provides a command to handle passing of the load trigger point.
/// </summary>
/// <param name="AgvCorrelationId">The agv correlation id.</param>
/// <param name="CorrelationId">The action group correlation id (if agv carried load).</param>
public record PassLoadTriggerPoint(Guid AgvCorrelationId, Guid CorrelationId) : CorrelatedBy<Guid>;