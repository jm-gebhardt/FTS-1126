using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{
    /// <summary>
    /// An event that provides a assigned AGV for an order 
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record AgvAssignmentSucceeded : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Identifier of the Assigned AGV
        /// </summary>
        public string AgvId { get; }

        /// <summary>
        /// The AGV correlation id 
        /// </summary>
        public Guid AgvCorrelationId { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="AgvAssignmentSucceeded"/>
        /// </summary>
        public AgvAssignmentSucceeded(Guid correlationId, string agvId, Guid agvCorrelationId)
        {
            CorrelationId = correlationId;
            AgvId = agvId;
            AgvCorrelationId = agvCorrelationId;
        }
    }
}
