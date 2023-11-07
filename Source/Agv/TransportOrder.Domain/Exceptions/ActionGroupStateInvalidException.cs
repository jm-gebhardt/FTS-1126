using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;

/// <summary>
/// ActionGroup state is not valid
/// </summary>
public class ActionGroupStateInvalidException : Exception
{
    /// <param name="actionGroup">ActionGroup</param>
    [UsedImplicitly]
    public ActionGroupStateInvalidException(ActionGroup actionGroup) : base($"ActionGroup State for {actionGroup.CorrelationId} is invalid. Action states: {string.Join(", ", actionGroup.Actions.Select(a => a.State))}")
    {
    }

    /// <inheritdoc cref="ActionGroupStateInvalidException"/>>
    public ActionGroupStateInvalidException(ActionGroup actionGroup, string message) : base($"ActionGroup State {actionGroup.State} is invalid. {message} ")
    {
    }
}