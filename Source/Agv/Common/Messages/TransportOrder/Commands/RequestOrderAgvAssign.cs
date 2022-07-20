using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands
{
    /// <summary>
    /// Request the assignment of an agv for given correlationId
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record RequestOrderAgvAssign : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        ///  RequestOrderAgvAssign Constructor
        /// </summary>
        /// <param name="correlationId"></param>
        public RequestOrderAgvAssign(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
