using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

/// <summary>
/// AGV assignment failed, which usually means that there was no AGV available
/// </summary>
public class AgvAssignmentFailed: CorrelatedBy<Guid>
{
    /// <summary>
    /// Order Correlation ID
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// AgvAssignmentFailed Constructor
    /// </summary>
    /// <param name="correlationId"></param>
    public AgvAssignmentFailed(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}