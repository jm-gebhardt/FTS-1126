using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

/// <summary>
/// Agv reservation request was rejected
/// </summary>
public sealed class AgvReservationRejected : CorrelatedBy<Guid>
{
    /// <summary>
    /// Agv Correlation Id
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// AgvReservationRejected Constructor
    /// </summary>
    /// <param name="correlationId"></param>
    public AgvReservationRejected(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}