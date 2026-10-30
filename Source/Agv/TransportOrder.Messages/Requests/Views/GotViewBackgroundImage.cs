namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Views;

/// <summary>
/// Response with the PNG background image of a view.
/// </summary>
/// <param name="Image">The image bytes. Null if the view has no background image.</param>
public record GotViewBackgroundImage(byte[]? Image);