using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Exceptions;

/// <summary>
/// Assigned Agv was not found in <see cref="AgvAction"/>
/// </summary>
public class AssignedAgvNotFoundException : Exception
{
    /// <summary>
    /// Assigned Agv was not found
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="action">The AgvAction where the Agv should be assigned to</param>
    [UsedImplicitly]
    public AssignedAgvNotFoundException(IAgv agv, AgvAction action)
        : base($"No assigned Agv for action with CorrelationId: {action.CorrelationId} " +
               $"AssignedAgv: {agv.CorrelationId} " +
               $"AgvId: {agv.AgvId}")
    {
    }

    /// <summary>
    /// Assigned Agv was not found
    /// </summary>
    /// <param name="agvId"></param>
    /// <param name="action">The AgvAction where the Agv should be assigned to</param>
    /// <param name="correlationId"></param>
    public AssignedAgvNotFoundException(Guid correlationId, string agvId, AgvAction action)
        : base($"No assigned Agv for action with CorrelationId: {action.CorrelationId} " +
               $"AssignedAgv: {correlationId} " +
               $"AgvId: {agvId}")
    {
    }
}