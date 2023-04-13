using Gebhardt.StoreWare.Agv.Common.Contracts;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos
{
    /// <summary>
    /// AgvAction model 
    /// </summary>
    public record AgvActionDto : CorrelatedBy<Guid>
    {
        /// <inheritdoc />
        public Guid CorrelationId { get; }

        /// <inheritdoc />
        public Guid ParentId { get; }

        /// <inheritdoc />
        public int Sequence { get; }

        /// <inheritdoc />
        public Guid? Successor { get; }

        /// <inheritdoc />
        public Guid? Predecessor { get; }
        
        /// <summary>
        /// The current state of the action
        /// </summary>
        public ActionState State { get; }

        /// <summary>
        /// The type of the action, e.g. Pick or Drop
        /// </summary>
        public ActionType ActionType { get; }

        /// <summary>
        /// Target node id
        /// </summary>
        public string TargetNodeId { get; }

        /// <summary>
        /// The nodes of the trip
        /// </summary>
        public string[] Nodes { get; } = Array.Empty<string>();

        /// <summary>
        /// AgvActionDto Constructor
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="parentId"></param>
        /// <param name="sequence"></param>
        /// <param name="predecessor"></param>
        /// <param name="successor"></param>
        /// <param name="assignedAgv"></param>
        /// <param name="state"></param>
        /// <param name="actionType"></param>
        /// <param name="targetNodeId"></param>
        /// <param name="nodes"></param>
        public AgvActionDto(Guid correlationId, Guid parentId, int sequence, Guid? predecessor, Guid? successor, ActionState state, ActionType actionType,  string targetNodeId, string[] nodes)
        {
            CorrelationId = correlationId;
            ParentId = parentId;
            Sequence = sequence;
            Predecessor = predecessor;
            Successor = successor;
            State = state;
            ActionType = actionType;
            TargetNodeId = targetNodeId;
            Nodes = nodes;
        }
    }
}
