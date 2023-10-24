using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Provides a request to get filtered action groups.
/// </summary>
/// <param name="States">The action group states filter.</param>
/// <param name="FromDate">The earliest creation date filter. </param>
/// <param name="ToDate">The latest creation date filter.</param>
/// <param name="Limit">The limit of returned action groups.</param>
/// <param name="SortBy">Sorting parameter.</param>
/// <param name="SortDescending">If true, sort action groups in descending order.</param>
public record GetActionGroups(ActionGroupState[] States, DateTime FromDate, DateTime ToDate, int Limit = 1000, SortBy SortBy=SortBy.CreatedDate, bool SortDescending = true);