namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Map;

/// <summary>
/// Defines a result for the GetMapImage request
/// </summary>
public record GotMapImage(byte[] Image);