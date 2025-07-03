using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.ReadModels;

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
/// <param name="Edges">The edges of the trip.</param>
/// <param name="CreatedDate">The creation date.</param>
/// <param name="StartedDate">The start date.</param>
/// <param name="CompletedDate">The completion date.</param>
/// <param name="PausedDate">The paused date.</param>
/// <param name="PausedDuration">The duration that the action was paused.</param>
/// <param name="IsSkipped">True, if the action group is skipped.</param>
/// <param name="InfoToAgv">Information that will be sent to the agv.</param>
/// <param name="AdditionalInfo">Information passed from or to external systems.</param>
public record AgvActionReadModel(
    Guid CorrelationId,
    Guid ParentId,
    int Sequence,
    ActionState State,
    ActionType ActionType,
    string TargetNodeId,
    AgvGraph.NodeType TargetNodeType,
    string[] Nodes,
    Guid[] Edges,
    DateTime CreatedDate,
    DateTime? StartedDate,
    [PublicAPI]DateTime? CompletedDate,
    [PublicAPI]DateTime? PausedDate,
    long PausedDuration,
    bool IsSkipped,
    Dictionary<InfoToAgvKey, string>? InfoToAgv = null,
    string? AdditionalInfo = null);