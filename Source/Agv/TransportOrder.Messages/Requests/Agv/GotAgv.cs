namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Defines a result for the <see cref="GetAgv"/>
/// </summary>
/// <param name="Agv">The returned Agv</param>
public record GotAgv(MessageContent.Agv Agv);