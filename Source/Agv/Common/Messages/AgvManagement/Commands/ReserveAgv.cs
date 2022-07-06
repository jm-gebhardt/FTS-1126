using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;

/// <summary>
/// Request an AGV to be reserved
/// </summary>
public sealed class ReserveAgv: CorrelatedBy<Guid>
{
    /// <summary>
    /// Agv Correlation ID
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// ReserveAgv
    /// </summary>
    /// <param name="correlationId"></param>
    public ReserveAgv(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}