using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Events;

/// <summary>
/// Represents the event raised when an agv went offline while reserving zones.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="SerialNumber">The serial number id of the agv.</param>
/// <param name="Zones">The zones it reserved.</param>
/// <param name="CurrentOnlineAgvs">Currently online agvs.</param>
[PublicAPI]
public record AgvWentOfflineInZones(Guid CorrelationId, string SerialNumber, ICollection<string> Zones, ICollection<string> CurrentOnlineAgvs) : CorrelatedBy<Guid>;