namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// Thrown when an ActionGroup is already assigned to an Agv
/// </summary>
public class ActionGroupAlreadyAssignedException : Exception
{
    /// <inheritdoc cref="ActionGroupAlreadyAssignedException"/>>
    /// <param name="actionGroupAssignedAgv"></param>
    /// <param name="correlationId"></param>
    public ActionGroupAlreadyAssignedException(Guid? actionGroupAssignedAgv, Guid correlationId) : base($"ActionGroup with correlation ID {correlationId} is already assigned to Agv {actionGroupAssignedAgv}")
    {
    }
}