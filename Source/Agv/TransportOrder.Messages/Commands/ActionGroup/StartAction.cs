using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to start an <see cref="Action"/> for an agv.
/// </summary>
/// <param name="Action">The action.</param>
/// <param name="AssignedAgv">The agv.</param>
public record StartAction(AgvAction Action, Guid AssignedAgv);