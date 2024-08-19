namespace Gebhardt.Acc.TransportOrder.Messages.Requests.RestrictedArea;

/// <summary>
/// Provides a request for getting a restricted area by its id.
/// </summary>
/// <param name="RestrictedAreaId">The id of the restricted area.</param>
public record GetRestrictedArea(Guid RestrictedAreaId);