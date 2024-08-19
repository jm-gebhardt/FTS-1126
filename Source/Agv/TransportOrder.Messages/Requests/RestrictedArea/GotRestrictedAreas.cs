namespace Gebhardt.Acc.TransportOrder.Messages.Requests.RestrictedArea;

/// <summary>
/// Represents the response to the <see cref="GetRestrictedAreas"/> request.
/// </summary>
/// <param name="RestrictedAreas">The restricted areas.</param>
public record GotRestrictedAreas(ICollection<Domain.Models.RestrictedArea.RestrictedArea> RestrictedAreas);