using Gebhardt.Acc.TransportOrder.Domain.Models.Views;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Views;

/// <summary>
/// Response with one view.
/// </summary>
/// <param name="View">The view.</param>
public record GotView(View View);