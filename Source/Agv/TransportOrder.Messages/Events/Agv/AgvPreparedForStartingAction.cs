
namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Represents the event raised after the agv was prepared for starting the action.
/// </summary>
/// <param name="Action">The action.</param>
/// <param name="AssignedAgv">The agv.</param>
public record AgvPreparedForStartingAction(Domain.Models.ActionGroup.AgvAction Action, Guid AssignedAgv);