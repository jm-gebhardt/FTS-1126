using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

/// <summary>
/// AGV reservation request was successful
/// </summary>
public sealed class AgvReservationSucceeded: CorrelatedBy<Guid>
{
    
    /// <summary>
    /// Correlation ID of the AGV
    /// </summary>
    public Guid CorrelationId { get; }
    
    /// <summary>
    /// AgvReservationSucceeded Constructor
    /// </summary>
    /// <param name="correlationId"></param>
    public AgvReservationSucceeded(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}