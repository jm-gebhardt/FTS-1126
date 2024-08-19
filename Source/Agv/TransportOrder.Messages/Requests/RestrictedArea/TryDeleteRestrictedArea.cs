namespace Gebhardt.Acc.TransportOrder.Messages.Requests.RestrictedArea;

/// <summary>
/// Provides a request to try to delete a restricted area by its id.
/// </summary>
/// <param name="RestrictedAreaId">The id of the restricted area.</param>
public record TryDeleteRestrictedArea(Guid RestrictedAreaId);