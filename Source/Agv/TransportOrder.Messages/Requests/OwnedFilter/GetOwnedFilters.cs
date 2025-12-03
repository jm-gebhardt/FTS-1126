namespace Gebhardt.Acc.TransportOrder.Messages.Requests.OwnedFilter;

/// <summary>
/// Represents a request to get a list of all the <see cref="OwnedFilter"/> of the owner.
/// </summary>
/// <param name="Owner">The owner to get the <see cref="OwnedFilter"/> for.</param>
public record GetOwnedFilters(string Owner);