using Gebhardt.Acc.Common.Messages.ActionGroups;

namespace Gebhardt.Acc.Common.Exceptions;

/// <summary>
/// Assigned Agv was not found in AgvAction
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

    /// <summary>
    /// Assigned Agv was not found.
    /// </summary>
    /// <param name="agvSerialNumber">The serial number of the agv.</param>
    /// <param name="correlationId">The correlation id of the instant action.</param>
    public AssignedAgvNotFoundException(string agvSerialNumber, Guid correlationId)
        : base($"No assigned Agv  {agvSerialNumber} for instant action  {correlationId} ")
    {
    }
}