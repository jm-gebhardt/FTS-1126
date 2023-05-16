namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// Thrown when an action group is already assigned to an AGV
/// </summary>
public class ActionGroupAlreadyAssignedException : Exception
{
    /// <inheritdoc cref="ActionGroupAlreadyAssignedException"/>>
    /// <param name="actionGroupAssignedAgv"></param>
    /// <param name="correlationId"></param>
    public ActionGroupAlreadyAssignedException(Guid? actionGroupAssignedAgv, Guid correlationId) : base($"Action group with correlation ID {correlationId} is already assigned to AGV {actionGroupAssignedAgv}")
    {
    }
}