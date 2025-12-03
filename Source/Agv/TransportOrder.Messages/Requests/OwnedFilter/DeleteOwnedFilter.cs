namespace Gebhardt.Acc.TransportOrder.Messages.Requests.OwnedFilter;

/// <summary>
/// Represents a request to delete an owned filter.
/// </summary>
/// <param name="Owner">The owner of the filter.</param>
/// <param name="OwnedFilterCorrelationId">The correlation id of the filter.</param>
public record DeleteOwnedFilter(string Owner, Guid OwnedFilterCorrelationId);