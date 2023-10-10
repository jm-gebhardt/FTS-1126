
namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.ActionGroup;

/// <summary>
/// Represents the event raised after nodes have been selected for an agv action.
/// </summary>
/// <param name="Action">The action.</param>
/// <param name="AssignedAgv">The agv.</param>
public record NodesSelectedForAction(Domain.Models.ActionGroup.AgvAction Action, Guid AssignedAgv);