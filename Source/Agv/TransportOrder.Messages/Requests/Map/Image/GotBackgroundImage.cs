namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Image;

/// <summary>Response containing the PNG bytes of the background image.</summary>
public record GotBackgroundImage(byte[] Image);