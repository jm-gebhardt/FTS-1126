using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries
{
    /// <summary>
    /// Represents the GetAgv request which returns a list of Agvs
    /// </summary>
    [JetBrains.Annotations.UsedImplicitly]
    public record GetAgvsRequest : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// <inheritdoc cref="GetAgvsRequest"/>
        /// </summary>
        /// <param name="correlationId"></param>
        public GetAgvsRequest(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}