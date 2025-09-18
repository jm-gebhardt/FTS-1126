using Gebhardt.Acc.Common.Messages.ActionGroups;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

internal static class ActionStateHelper
{
    public static void SetActionState(ActionGroup actionGroup, ActionState state)
    {
        foreach (var action in actionGroup.Actions)
        {
            action.UpdateActionState(state, DateTimeOffset.Now);
        }
    }
}