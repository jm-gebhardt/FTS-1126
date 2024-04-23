namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;

/// <summary>
/// Defines a result for the <see cref="GetAgvs"/>
/// </summary>
/// <param name="Agvs">The returned agvs.</param>
public record GotAgvs(ICollection<MessageContent.Agv> Agvs);