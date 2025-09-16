using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Domain.ReadModels;

/// <summary>
/// Provides extension methods for testing the action group read model.
/// </summary>
public static class ActionGroupReadModelExtension
{
    /// <summary>
    /// Converts an action group to its read model with node types using made-up fake node types.
    /// </summary>
    /// <param name="actionGroup">The action group to be converted.</param>
    public static ActionGroupReadModel? ConvertFromActionGroupWithFakeNodeType(this ActionGroup? actionGroup)
    {
        if (actionGroup == null)
        {
            return null;
        }

        return new ActionGroupReadModel(actionGroup.CorrelationId, actionGroup.OrderId, actionGroup.AssignedAgv,
            actionGroup.CreatedDate, actionGroup.StartedDate, actionGroup.CompletedDate, actionGroup.EstimatedDuration, actionGroup.State, actionGroup.Creator, actionGroup.HandlingUnit,
            actionGroup.Actions.Select(a => new AgvActionReadModel(a.CorrelationId, a.ParentId, a.Sequence, a.State,
                a.ActionType, a.Target.Node?.Id, a.Target.Node?.Name, AgvGraph.NodeType.Charge, a.Nodes.Select(n => n.Name).ToArray(), a.Edges, a.CreatedDate, a.StartedDate, a.CompletedDate, a.PausedDate, a.PriorRunAndPauseDuration, a.ShouldSkip)).ToList(), actionGroup.IsManual, actionGroup.HasPriority, actionGroup.IsQueued, actionGroup.Reasons, actionGroup.DoNotPause);
    }

    /// <summary>
    /// Converts an action group to its read model with node types using a given node type.
    /// </summary>
    /// <param name="actionGroup">The action group to be converted.</param>
    /// <param name="nodeType">The node type to be used during conversion</param>
    public static ActionGroupReadModel? ConvertFromActionGroupWithGivenNodeType(this ActionGroup? actionGroup, AgvGraph.NodeType nodeType)
    {
        if (actionGroup == null)
        {
            return null;
        }

        return new ActionGroupReadModel(actionGroup.CorrelationId, actionGroup.OrderId, actionGroup.AssignedAgv,
            actionGroup.CreatedDate, actionGroup.StartedDate, actionGroup.CompletedDate, actionGroup.EstimatedDuration, actionGroup.State, actionGroup.Creator, actionGroup.HandlingUnit,
            actionGroup.Actions.Select(a => new AgvActionReadModel(a.CorrelationId, a.ParentId, a.Sequence, a.State,
                a.ActionType, a.Target.Node!.Id, a.Target.Node.Name, nodeType, a.Nodes.Select(n => n.Name).ToArray(), a.Edges, a.CreatedDate, a.StartedDate, a.CompletedDate, a.PausedDate, a.PriorRunAndPauseDuration, a.ShouldSkip)).ToList(), actionGroup.IsManual, actionGroup.HasPriority, actionGroup.IsQueued, actionGroup.Reasons, actionGroup.DoNotPause);
    }
}