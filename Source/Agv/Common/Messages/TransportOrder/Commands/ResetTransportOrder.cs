using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands
{
    /// <summary>
    /// A command that resets the state of an order so it will be executed again
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record ResetTransportOrder : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ResetTransportOrder"/>
        /// </summary>
        public ResetTransportOrder(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
