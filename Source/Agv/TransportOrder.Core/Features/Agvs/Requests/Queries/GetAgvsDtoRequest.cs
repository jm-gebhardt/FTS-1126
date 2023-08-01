namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries
{
    /// <summary>
    /// Represents the GetAgv request which returns a list of Agvs
    /// </summary>
    [JetBrains.Annotations.UsedImplicitly]
    public record GetAgvsDtoRequest(ICollection<Guid> CorrelationIds);
}