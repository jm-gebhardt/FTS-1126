using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands
{
    /// <summary>
    /// Command to request the assignment of an AGV for a specific order.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record AssignAgv : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Order Details
        /// </summary>
        public Order Order { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="AssignAgv"/>
        /// </summary>
        public AssignAgv(Guid correlationId, Order order)
        {
            CorrelationId = correlationId;
            Order = order;
        }
    }
}
