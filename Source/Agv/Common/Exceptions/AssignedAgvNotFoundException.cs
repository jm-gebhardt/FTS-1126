using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;

namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// Assigned Agv was not found in <see cref="AgvAction"/>
/// </summary>
public class AssignedAgvNotFoundException : Exception
{
    /// <summary>
    /// Assigned Agv was not found
    /// </summary>
    /// <param name="agvSerialNumber">The serial number of the agv.</param>
    /// <param name="action">The AgvAction where the Agv should be assigned to</param>
    /// <param name="correlationId"></param>
    public AssignedAgvNotFoundException(Guid correlationId, string agvSerialNumber, AgvAction action)
        : base($"No assigned Agv for action with CorrelationId: {action.CorrelationId} " +
               $"AssignedAgv: {correlationId} " +
               $"AgvSerialNumber: {agvSerialNumber}")
    {
    }
}