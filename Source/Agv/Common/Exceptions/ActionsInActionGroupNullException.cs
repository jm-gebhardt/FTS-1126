namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// Exception thrown when Actions in ActionGroup are null
/// </summary>
public class ActionsInActionGroupNullException : Exception
{
    /// <inheritdoc cref="ActionsInActionGroupNullException"/>
    /// <param name="actionGroupCorrelationId"></param>
    public ActionsInActionGroupNullException(Guid actionGroupCorrelationId) : base($"ActionGroup with correlationId {actionGroupCorrelationId} has null Actions")
    {
    }
}