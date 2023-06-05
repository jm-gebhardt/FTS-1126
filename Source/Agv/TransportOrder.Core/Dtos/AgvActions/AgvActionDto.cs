using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos.AgvActions
{
    /// <summary>
    /// AgvAction model
    /// </summary>
    [PublicAPI]
    public record AgvActionDto
    {
        /// <summary>
        /// 
        /// </summary>
        public Guid CorrelationId { get; }

        /// <summary>
        /// 
        /// </summary>
        public Guid ParentId { get; }

        /// <summary>
        /// 
        /// </summary>
        public int Sequence { get; }

        /// <summary>
        /// 
        /// </summary>
        public ActionState State { get; }

        /// <summary>
        /// 
        /// </summary>
        public ActionType ActionType { get; }

        /// <summary>
        /// 
        /// </summary>
        public string TargetNodeId { get; }

        /// <summary>
        /// 
        /// </summary>
        public string[] Nodes { get; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime CreatedDate { get; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime? StartedDate { get; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime? CompletedDate { get; }

        /// <summary>
        /// AgvAction model
        /// <param name="correlationId">Correlation ID</param>
        /// <param name="parentId">Action group ID</param>
        /// <param name="sequence">Sequence number</param>
        /// <param name="state">The current state of the action</param>
        /// <param name="actionType">The type of the action, e.g. Pick or Drop</param>
        /// <param name="targetNodeId">Target node id</param>
        /// <param name="nodes">The nodes of the trip</param>
        /// <param name="createdDate">Date the action was created (UTC)</param>
        /// <param name="startedDate">Date the action was started (UTC)</param>
        /// <param name="completedDate">Date the action was completed (UTC)</param>
        /// </summary>
        public AgvActionDto
        (
            Guid correlationId,
            Guid parentId,
            int sequence,
            ActionState state,
            ActionType actionType,
            string targetNodeId,
            string[] nodes,
            DateTime createdDate,
            DateTime? startedDate,
            DateTime? completedDate
        )
        {
            CorrelationId = correlationId;
            ParentId = parentId;
            Sequence = sequence;
            State = state;
            ActionType = actionType;
            TargetNodeId = targetNodeId;
            Nodes = nodes;
            CreatedDate = createdDate;
            StartedDate = startedDate;
            CompletedDate = completedDate;
        }

        /// <summary>
        /// AutoMapper ctor
        /// </summary>
        public AgvActionDto()
        {
            TargetNodeId = null!;
            Nodes = null!;
        }
    }
}
