using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Represents the GetAgv request which returns a list of Agvs
/// </summary>
[UsedImplicitly]
public record GetAgvs(ICollection<Guid> CorrelationIds);