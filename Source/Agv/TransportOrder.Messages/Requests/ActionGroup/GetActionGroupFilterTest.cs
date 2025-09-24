using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.Filter.Enums;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// 
/// </summary>
public record GetFilteredActionGroups(FilterBaseDto Filter, int PageNumber, int PageSize, SortOrdering Ordering, ICollection<string> SortProperties);