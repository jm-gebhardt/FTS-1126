using Gebhardt.Acc.TransportOrder.Messages.MessageContent;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;

/// <summary>
/// Defines a result for the GetAgvGraphs request.
/// </summary>
public record GotAgvGraphs(List<Graph>? Graphs);