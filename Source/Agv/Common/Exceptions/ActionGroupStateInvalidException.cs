using Gebhardt.StoreWare.Agv.Common.Messages.Actions;

namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// ActionGroup state is not valid
/// </summary>
public class ActionGroupStateInvalidException : Exception
{
    /// <see cref="ActionGroupStateInvalidException"/>>
    /// <param name="actionGroup">ActionGroup</param>
    [JetBrains.Annotations.UsedImplicitly]
    public ActionGroupStateInvalidException(ActionGroup actionGroup) : base($"ActionGroup State for {actionGroup.CorrelationId} is invalid")
    {
        
    }
}