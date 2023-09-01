using Gebhardt.StoreWare.Agv.Common.Messages.Actions;

namespace TransportOrder.Domain.Exceptions;

/// <summary>
/// ActionGroup state is not valid
/// </summary>
public class ActionGroupStateInvalidException : Exception
{
    /// <param name="actionGroup">ActionGroup</param>
    [JetBrains.Annotations.UsedImplicitly]
    public ActionGroupStateInvalidException(ActionGroup actionGroup) : base($"ActionGroup State for {actionGroup.CorrelationId} is invalid. Action states: {string.Join(", ", actionGroup.Actions.Select(a => a.State))}")
    {
    }
}