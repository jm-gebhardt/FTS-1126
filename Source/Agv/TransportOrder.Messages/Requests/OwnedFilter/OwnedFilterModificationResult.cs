namespace Gebhardt.Acc.TransportOrder.Messages.Requests.OwnedFilter;

/// <summary>
/// Represents a response to <see cref="UpdateOwnedFilter"/> and <see cref="DeleteOwnedFilter"/>.
/// </summary>
/// <param name="Result">The result of the operation.</param>
public record OwnedFilterModificationResult(bool Result);