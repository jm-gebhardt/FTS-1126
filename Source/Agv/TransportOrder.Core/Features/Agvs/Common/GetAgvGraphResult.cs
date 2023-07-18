using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos.Graphs;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common
{
    /// <summary>
    /// Defines a result for the GetAgvGraph request
    /// </summary>
    public record GetAgvGraphResult(GraphDto AgvGraph);
}
