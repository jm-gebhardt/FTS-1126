using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using ActionGroup = TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace TransportOrder.Domain.ReadModels;

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
            actionGroup.CreatedDate, actionGroup.StartedDate, actionGroup.CompletedDate, actionGroup.State, actionGroup.Creator,
            actionGroup.Actions.Select(a => new AgvActionReadModel(a.CorrelationId, a.ParentId, a.Sequence, a.State,
                a.ActionType, a.TargetNodeId, AgvGraph.NodeType.Charge, a.Nodes, a.CreatedDate, a.StartedDate, a.CompletedDate)).ToList(), actionGroup.IsManual);
    }
}


