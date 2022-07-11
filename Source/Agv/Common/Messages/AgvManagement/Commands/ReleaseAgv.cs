using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;

/// <summary>
/// Moves agv into idle state
/// </summary>
[JetBrains.Annotations.PublicAPI]
public class ReleaseAgv: CorrelatedBy<Guid>
{
    /// <summary>
    /// Agv Correlation ID
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// Build message.
    /// </summary>
    /// <param name="correlationId"></param>
    public ReleaseAgv(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}