namespace Gebhardt.Acc.TransportOrder.Messages.Requests.OwnedFilter;

/// <summary>
/// Represents a response to <see cref="AddOwnedFilter"/>.
/// </summary>
/// <param name="OwnedFilterCorrelationId">The correlation id of the added filter.</param>
public record AddedOwnedFilter(Guid OwnedFilterCorrelationId);