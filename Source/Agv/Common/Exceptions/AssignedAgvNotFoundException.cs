using Gebhardt.StoreWare.Agv.Common.Messages.Actions;

namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// Assigned Agv was not found in <see cref="AgvAction"/>
/// </summary>
public class AssignedAgvNotFoundException : Exception
{
    /// <summary>
    /// Assigned Agv was not found
    /// </summary>
    /// <param name="action">The AgvAction where the Agv should be assigned to</param>
    public AssignedAgvNotFoundException(AgvAction action)
        : base($"No assigned Agv for action with correlation Id: {action.CorrelationId} " +
               $"AssignedAgv: {action.AssignedAgv} " +
               $"AgvId: {action.AgvId}")
    {
    }
}