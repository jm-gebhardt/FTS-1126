using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using ActionGroup = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Extensions;

internal static class ActionGroupExtensions
{
    public static void SetActionState(this ActionGroup actionGroup, ActionState state) => ActionStateHelper.SetActionState(actionGroup, state);
}