using Gebhardt.StoreWare.Agv.Common.Contracts;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions
{
    /// <summary>
    /// Agv Action
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record AgvAction : CorrelatedBy<Guid>, IParentedBy<Guid>, ISaga
    {
        /// <summary>
        /// Unique correlation ID of this action (see order action repository)
        /// </summary>
        public Guid CorrelationId { get; set; }

        /// <inheritdoc/>
        public Guid ParentId { get; }

        /// <inheritdoc/>
        public int Sequence { get; init; }

        /// <inheritdoc/>
        public Guid Successor { get; init; }

        /// <inheritdoc/>
        public Guid Predecessor { get; init; }

        /// <summary>
        /// AGV ID
        /// </summary>
        public string? AgvId { get; init; }

        /// <summary>
        /// The CorrelationId of the assigned agv
        /// </summary>
        public Guid? AssignedAgv { get; set; }

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
        /// Target node id of the action
        /// </summary>
        public string TargetNodeId { get; init; }

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
        /// <param name="targetNodeId">Target node id of the action</param>
        public AgvAction(Guid correlationId, string agvId, ActionType actionType, string orderId, Guid parentId, string targetNodeId)
        {
            CorrelationId = correlationId;
            AgvId = agvId;
            ActionType = actionType;
            OrderId = orderId;
            ParentId = parentId;
            TargetNodeId = targetNodeId;
        }

        /// <summary>
        /// Just to satisfy Entity Framework
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        private AgvAction()
        {
            CorrelationId = Guid.Empty;
            AgvId = string.Empty;
            ActionType = ActionType.Pick;
            OrderId = string.Empty;
            ParentId = Guid.Empty;
            TargetNodeId = string.Empty;
        }
    }
}