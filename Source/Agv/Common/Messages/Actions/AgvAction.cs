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

        /// <summary>
        /// The action group this action belongs to
        /// </summary>
        public ActionGroup ActionGroup { get; set; }

        /// <inheritdoc/>
        public Guid ParentId { get; init; }

        /// <inheritdoc/>
        public int Sequence { get; init; }

        /// <inheritdoc/>
        public Guid Successor { get; init; }

        /// <inheritdoc/>
        public Guid Predecessor { get; init; }

        /// <summary>
        /// The current state of the action
        /// </summary>
        public ActionState State { get; set; } = ActionState.Created;

        /// <summary>
        /// The type of the action, e.g. Pick or Drop
        /// </summary>
        public ActionType ActionType { get; init; }

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
        /// <param name="actionType">The type of the action, e.g. Pick or Drop</param>
        /// <param name="actionGroup">The parent ActionGroup</param>
        /// <param name="targetNodeId">Target node id of the action</param>
        public AgvAction(Guid correlationId, ActionType actionType, ActionGroup actionGroup, string targetNodeId)
        {
            CorrelationId = correlationId;
            ActionType = actionType;
            ActionGroup = actionGroup;
            TargetNodeId = targetNodeId;
        }

        /// <summary>
        /// Just to satisfy Entity Framework
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        private AgvAction()
        {
            CorrelationId = Guid.Empty;
            ActionType = ActionType.Pick;
            ActionGroup = null!;
            TargetNodeId = string.Empty;
        }
    }
}