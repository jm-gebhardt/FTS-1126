using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Test.Extensions;

internal static class ActionGroupExtensions
{
    public static void SetActionState(this ActionGroup actionGroup, ActionState state) => ActionStateHelper.SetActionState(actionGroup, state);
}