using Gebhardt.Acc.TransportOrder.Messages.MessageContent;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Defines a result for the <see cref="GetAgvPosition"/> request.
/// </summary>
public record GotAgvPosition(AgvPositionStatus? AgvPositions);