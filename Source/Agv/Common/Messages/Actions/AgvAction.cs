using Gebhardt.StoreWare.Agv.Common.Contracts;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions
{
    /// <summary>
    /// Agv Action
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record AgvAction : CorrelatedBy<Guid>, IParentedBy<Guid>
    {
        /// <summary>
        /// Unique correlation ID of this action (see order action repository)
        /// </summary>
        public Guid CorrelationId { get; }

        /// <inheritdoc/>
        public Guid ParentId { get; }

        /// <inheritdoc/>
        public int Sequence { get; }

        /// <inheritdoc/>
        public Guid Successor { get; }

        /// <inheritdoc/>
        public Guid Predecessor { get; }

        /// <summary>
        /// AGV ID
        /// </summary>
        public string AgvId { get; init; }

        /// <summary>
        /// The CorrelationId of the assigned agv
        /// </summary>
        public Guid AgvCorrelationId { get; set; }

        /// <summary>
        /// The current state of the action
        /// </summary>
        public ActionState State { get; set; } = ActionState.Created;

        /// <summary>
        /// The type of the action, e.g. Pick or Drop
        /// </summary>
        public ActionType ActionType { get; init; }

        /// <summary>
        /// Order ID of the order that clamps all the corresponding actions
        /// </summary>
        public string OrderId { get; init; }

        /// <summary>
        /// Source of the trip, null when unknown (last node ID will be assumed)
        /// </summary>
        public string? Source { get; init; }

        /// <summary>
        /// Destination of the trip
        /// </summary>
        public string Destination { get; init; }

        /// <summary>
        /// The nodes of the trip
        /// </summary>
        public string[] Nodes { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="correlationId">Unique correlation ID of this action (see order action repository)</param>
        /// <param name="agvId">AGV ID</param>
        /// <param name="actionType">The type of the action, e.g. Pick or Drop</param>
        /// <param name="orderId">Order ID</param>
        /// <param name="parentId">The id of the clamped object</param>
        /// <param name="source">Source of the trip, null when unknown (last node ID will be assumed)</param>
        /// <param name="destination">Destination of the trip</param>
        public AgvAction(Guid correlationId, string agvId, ActionType actionType, string orderId, Guid parentId, string? source, string destination)
        {
            CorrelationId = correlationId;
            AgvId = agvId;
            ActionType = actionType;
            OrderId = orderId;
            ParentId = parentId;
            Source = source;
            Destination = destination;
        }
    }
}