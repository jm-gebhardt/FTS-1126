using Gebhardt.StoreWare.Agv.Common.Contracts;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos.AgvActions
{
    /// <summary>
    /// AgvAction model
    /// <param name="CorrelationId">Correlation ID</param>
    /// <param name="ParentId">Action group ID</param>
    /// <param name="Sequence">Sequence number</param>
    /// <param name="State">The current state of the action</param>
    /// <param name="ActionType">The type of the action, e.g. Pick or Drop</param>
    /// <param name="TargetNodeId">Target node id</param>
    /// <param name="Nodes">The nodes of the trip</param>
    /// <param name="CreatedDate">Date the action was created (UTC)</param>
    /// <param name="StartedDate">Date the action was started (UTC)</param>
    /// <param name="CompletedDate">Date the action was completed (UTC)</param>
    /// </summary>
    public record AgvActionDto(
        Guid CorrelationId,
        Guid ParentId,
        int Sequence,
        ActionState State,
        ActionType ActionType,
        string TargetNodeId, 
        string[] Nodes, 
        DateTime CreatedDate, 
        DateTime? StartedDate,
        DateTime? CompletedDate
    )  : CorrelatedBy<Guid>, IParentedBy<Guid>;
}
