using Gebhardt.Acc.TransportOrder.Messages.MessageContent;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;

/// <summary>
/// Defines a result for the GetAgvPositions request
/// </summary>
public record GotAgvPositions(List<AgvPositionStatus>? AgvPositions);