using Gebhardt.StoreWare.Agv.Common.Contracts;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos
{
    /// <summary>
    /// AgvAction model 
    /// </summary>
    public record AgvActionDto : CorrelatedBy<Guid>, IParentedBy<Guid>
    {
        /// <inheritdoc />
        public Guid CorrelationId { get; }

        /// <inheritdoc />
        public Guid ParentId { get; }

        /// <inheritdoc />
        public int Sequence { get; }

        /// <inheritdoc />
        public Guid Successor { get; }

        /// <inheritdoc />
        public Guid Predecessor { get; }
        
        /// <summary>
        /// The CorrelationId of the assigned agv
        /// </summary>
        public Guid? AssignedAgv { get; }

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
        public string NodeId { get; }

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
        /// <param name="nodeId"></param>
        /// <param name="nodes"></param>
        public AgvActionDto(Guid correlationId, Guid parentId, int sequence, Guid predecessor, Guid successor, Guid? assignedAgv, ActionState state, ActionType actionType,  string nodeId, string[] nodes)
        {
            CorrelationId = correlationId;
            ParentId = parentId;
            Sequence = sequence;
            Predecessor = predecessor;
            Successor = successor;
            AssignedAgv = assignedAgv;
            State = state;
            ActionType = actionType;
            NodeId = nodeId;
            Nodes = nodes;
        }
    }
}
