namespace Gebhardt.Acc.TransportOrder.Messages.Requests.RestrictedArea;

/// <summary>
/// Provides a request to get multiple or all (using an empty id collection) restricted areas.
/// </summary>
/// <param name="RestrictedAreaIds">The ids of the restricted areas.</param>
public record GetRestrictedAreas(ICollection<Guid> RestrictedAreaIds);