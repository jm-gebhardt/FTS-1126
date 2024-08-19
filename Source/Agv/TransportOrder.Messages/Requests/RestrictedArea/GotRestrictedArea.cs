namespace Gebhardt.Acc.TransportOrder.Messages.Requests.RestrictedArea;

/// <summary>
/// Represents the response to the <see cref="GetRestrictedArea"/> request.
/// </summary>
/// <param name="RestrictedArea"></param>
public record GotRestrictedArea(Domain.Models.RestrictedArea.RestrictedArea RestrictedArea);