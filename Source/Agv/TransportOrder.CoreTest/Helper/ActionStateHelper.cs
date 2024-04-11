using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using ActionGroup = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;

internal static class ActionStateHelper
{
    public static void SetActionState(ActionGroup actionGroup, ActionState state)
    {
        foreach (var action in actionGroup.Actions)
        {
            action.UpdateActionState(state);
        }
    }
}