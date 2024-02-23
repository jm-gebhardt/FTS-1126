using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;

/// <summary>
/// Defines a result for the <see cref="GetAgvsData"/> request.
/// </summary>
public record GotAgvsData(List<AgvData> AgvData);