using Gebhardt.Acc.Common.Filter.Api.Dtos;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.OwnedFilter;

/// <summary>
/// Represents a request to update an owned filter.
/// </summary>
/// <param name="Owner">The owner.</param>
/// <param name="UpdatedContent">The new content.</param>
/// <param name="OwnedFilterCorrelationId">The correlation id of the owned filter.</param>
/// <param name="Name">The new name of the owned filter.</param>
public record UpdateOwnedFilter(string Owner, FilterDto UpdatedContent, Guid OwnedFilterCorrelationId, string Name);