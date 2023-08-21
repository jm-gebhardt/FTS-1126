using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.ReadModels;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;

public static class ActionGroupReadModelExtension
{
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


