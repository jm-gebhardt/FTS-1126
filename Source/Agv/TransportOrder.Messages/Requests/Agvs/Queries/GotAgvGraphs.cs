namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;

/// <summary>
/// Defines a result for the GetAgvGraphs request.
/// </summary>
public record GotAgvGraphs(List<MessageContent.Graph>? Graphs);