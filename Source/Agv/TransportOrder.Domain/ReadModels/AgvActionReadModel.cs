using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;

/// <summary>
/// Represents a read model for agv actions.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action.</param>
/// <param name="ParentId">The correlation id of the parent action group.</param>
/// <param name="Sequence">The position in the sequence of actions.</param>
/// <param name="State"> The current state of the action.</param>
/// <param name="ActionType">The type of the action.</param>
/// <param name="TargetNodeId">The target node id of the action.</param>
/// <param name="TargetNodeType">The type of the target node.</param>
/// <param name="Nodes">The nodes of the trip.</param>
/// <param name="CreatedDate">The creation date.</param>
/// <param name="StartedDate">The start date.</param>
/// <param name="CompletedDate">The completion date.</param>
public record AgvActionReadModel(
    Guid CorrelationId,
    Guid ParentId,
    int Sequence,
    ActionState State,
    ActionType ActionType,
    string TargetNodeId,
    AgvGraph.NodeType TargetNodeType,
    string[] Nodes,
    DateTime CreatedDate,
    DateTime? StartedDate,
    DateTime? CompletedDate);