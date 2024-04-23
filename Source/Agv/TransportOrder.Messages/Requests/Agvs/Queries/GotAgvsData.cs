using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;

/// <summary>
/// Defines a result for the <see cref="GetAgvsData"/> request.
/// </summary>
public record GotAgvsData(List<AgvData> AgvData);