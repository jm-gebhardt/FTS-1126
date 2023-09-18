using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;

/// <summary>
/// The result of <see cref="GetAgvRequest"/>
/// </summary>
/// <param name="Agv"></param>
public record GetAgvResult(IAgv Agv);
