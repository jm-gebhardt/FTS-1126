namespace Gebhardt.Acc.TransportOrder.Messages.Requests.OwnedFilter;

/// <summary>
/// Represents a response to <see cref="GetOwnedFilters"/>.
/// </summary>
/// <param name="Filters">The filters of the user.</param>
public record GotOwnedFilters(ICollection<Domain.Models.Filter.OwnedFilter> Filters);