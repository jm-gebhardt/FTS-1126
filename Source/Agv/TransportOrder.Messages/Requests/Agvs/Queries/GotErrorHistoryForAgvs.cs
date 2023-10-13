using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;
/// <summary>
/// Defines a result for the <see cref="GetErrorHistoryForAgvs"/> request.
/// </summary>
public record GotErrorHistoryForAgvs(Dictionary<Guid, List<AgvError>> ErrorsByAgv);