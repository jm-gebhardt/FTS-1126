using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;

/// <summary>
/// Thrown when an ActionGroup is already assigned to an Agv
/// </summary>
public class ActionGroupAlreadyAssignedException : Exception
{
    /// <inheritdoc cref="ActionGroupAlreadyAssignedException"/>>
    /// <param name="actionGroup"></param>
    /// <param name="tryingToAssignAgvCorrelationId"></param>
    public ActionGroupAlreadyAssignedException(ActionGroup actionGroup, Guid tryingToAssignAgvCorrelationId)
        : base($"ActionGroup with CorrelationId {actionGroup.CorrelationId} {actionGroup.Actions.First().ActionType} to {actionGroup.Actions.First().TargetNodeId} is already assigned to Agv {actionGroup.AssignedAgv}, we wanted to assign Agv {tryingToAssignAgvCorrelationId}")
    {
    }
}