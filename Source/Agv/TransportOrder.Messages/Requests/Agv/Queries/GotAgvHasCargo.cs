using Gebhardt.Acc.TransportOrder.Messages.MessageContent;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Response to get the has agv cargo
/// </summary>
/// <param name="AgvHasCargo">AgvHasCargo</param>
public record GotAgvHasCargo(AgvHasCargo AgvHasCargo);