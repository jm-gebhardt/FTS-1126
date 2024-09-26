using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Node;

/// <summary>
///Provides a command to lock a station.
/// </summary>
/// <param name="CorrelationId">The station to lock.</param>
/// <param name="User">The user that requested the locking.</param>
/// <param name="Reason">The reason for the locking.</param>
/// <param name="LockDescription">Description for the locking.</param>
/// <param name="LockedOrderHandling">Handling policy for orders.</param>
public record LockStation(Guid CorrelationId, string User, string Reason, string LockDescription, LockedOrderHandlingType LockedOrderHandling): CorrelatedBy<Guid>;