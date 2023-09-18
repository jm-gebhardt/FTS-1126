namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Defines a result for the <see cref="GetAgvs"/>
/// </summary>
/// <param name="Agvs">The returned agvs.</param>
/// <param name="AreInitialState">If the agv with the same index is in the initial state.</param>
public record GotAgvs(ICollection<MessageContent.Agv> Agvs, ICollection<bool> AreInitialState);