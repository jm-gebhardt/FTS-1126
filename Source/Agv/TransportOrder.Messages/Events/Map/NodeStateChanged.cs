using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Map;

/// <summary>
/// Represents the event raised when the state of a node changed.
/// </summary>
/// <param name="NodeLock">The node lock containing node state.</param>
/// <param name="NodeName">The name of the node.</param>
/// <param name="StationId">The id of the station.</param>
/// <param name="ModifyingUser">User editing station if applicable.</param>
/// <param name="LockReason">Reason station was locked if applicable.</param>
/// <param name="LockDescription">Description of reason station was locked if applicable.</param>
public record NodeStateChanged(NodeLock NodeLock, string NodeName, Guid? StationId, string? ModifyingUser, string? LockReason, string? LockDescription);