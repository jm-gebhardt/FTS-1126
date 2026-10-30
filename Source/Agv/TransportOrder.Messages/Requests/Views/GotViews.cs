using Gebhardt.Acc.TransportOrder.Domain.Models.Views;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Views;

/// <summary>
/// Response with views.
/// </summary>
/// <param name="Views">The views.</param>
public record GotViews(IReadOnlyList<View> Views);