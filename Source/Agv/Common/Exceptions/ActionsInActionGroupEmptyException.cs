namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// Exception thrown when Actions in ActionGroup are null
/// </summary>
public class ActionsInActionGroupEmptyException : Exception
{
    /// <inheritdoc cref="ActionsInActionGroupEmptyException"/>
    /// <param name="actionGroupCorrelationId"></param>
    public ActionsInActionGroupEmptyException(Guid actionGroupCorrelationId) : base($"ActionGroup with CorrelationId {actionGroupCorrelationId} has null Actions")
    {
    }
}