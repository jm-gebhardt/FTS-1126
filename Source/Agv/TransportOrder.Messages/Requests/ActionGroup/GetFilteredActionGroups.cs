using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.Filter.Enums;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Represents a request to get filtered action groups.
/// </summary>
/// <param name="Filter">The filter to be used for filtering</param>
/// <param name="PageNumber">The amount of pages to get.</param>
/// <param name="PageSize">The page size.</param>
/// <param name="Direction">The sort direction.</param>
/// <param name="SortProperties">The properties to sort by.</param>
public record GetFilteredActionGroups(FilterDto Filter, int PageNumber, int PageSize, SortDirection Direction, ICollection<string> SortProperties);