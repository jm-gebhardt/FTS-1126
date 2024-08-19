namespace Gebhardt.Acc.TransportOrder.Messages.Requests.RestrictedArea;

/// <summary>
/// Provides the response to the <see cref="AddRestrictedArea"/> request.
/// </summary>
/// <param name="RestrictedArea">The new restricted area.</param>
public record RestrictedAreaAdded(Domain.Models.RestrictedArea.RestrictedArea RestrictedArea);