using Gebhardt.Acc.TransportOrder.Messages.MessageContent;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Response to get the has cargo state for the specified AgvCorrelationIds.
/// </summary>
/// <param name="AgvsHasCargo">List of AgvHasCargo for all agvs.</param>
public record GotAgvsHasCargo(List<AgvHasCargo> AgvsHasCargo);