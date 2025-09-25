using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.Filter.Enums;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Represents a request to get filtered action groups.
/// </summary>
/// <param name="Filter">The filter to be used for filtering</param>
/// <param name="PageNumber">The amount of pages to get.</param>
/// <param name="PageSize">The page size.</param>
/// <param name="Ordering">The sort direction.</param>
/// <param name="SortProperties">The properties to sort by.</param>
public record GetFilteredActionGroups(FilterBaseDto Filter, int PageNumber, int PageSize, SortOrdering Ordering, ICollection<string> SortProperties);