using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;

/// <summary>
/// Represents a request to lock a station.
/// </summary>
/// <param name="CorrelationId">The station to lock.</param>
/// <param name="User">The user that requested the locking.</param>
/// <param name="Reason">The reason for the locking.</param>
/// <param name="LockDescription">Description for the locking.</param>
/// <param name="LockedOrderHandling">How orders are supposed to be handled that involve the locked station.</param>
public record LockStation(Guid CorrelationId, string User, string Reason, string LockDescription, LockedOrderHandlingType LockedOrderHandling): CorrelatedBy<Guid>;